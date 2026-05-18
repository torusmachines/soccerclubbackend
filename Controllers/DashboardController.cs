using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly FootballContext _footballContext;
    private readonly AppDbContext _appDbContext;
    private readonly IPlayerPositionService _playerPositionService;
    private readonly ICompanyProfileRepository _companyProfileRepository;

    public DashboardController(
        FootballContext footballContext,
        AppDbContext appDbContext,
        IPlayerPositionService playerPositionService,
        ICompanyProfileRepository companyProfileRepository)
    {
        _footballContext = footballContext;
        _appDbContext = appDbContext;
        _playerPositionService = playerPositionService;
        _companyProfileRepository = companyProfileRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
        // Return a single object containing dashboard counters:
        // - dashboardTotalPlayers
        // - dashboardTotalExpiringContracts (within next 2 months)
        // - dashboardTotalOpenTasks
        // - dashboardNeedsReview (players without any review)

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddMonths(2);

        var totalPlayers = await _footballContext.Players1
            .AsNoTracking()
            .CountAsync();

        // Detect if logged-in user is a Player and resolve their player record (by email)
        var roleValues = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        var isPlayerUser = roleValues.Any(r => string.Equals(r, "Player", StringComparison.OrdinalIgnoreCase))
            || User.IsInRole("Player");

        string? loggedInPlayerId = null;
        if (isPlayerUser)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var player = await _footballContext.Players1
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.playerEmail != null && p.playerEmail.ToLower() == userEmail.ToLower());

                if (player != null)
                {
                    loggedInPlayerId = player.PlayerId;
                }
            }
        }

        // Counters: for players, show counts scoped to that player only
        var totalExpiringContracts = 0;
        var totalOpenTasks = 0;
        var totalNeedsReview = 0;

        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            totalExpiringContracts = await _footballContext.Players1
                .AsNoTracking()
                .Where(p => p.PlayerId == loggedInPlayerId
                            && p.ContractEndDate != null && p.ContractEndDate >= today && p.ContractEndDate <= cutoff)
                .CountAsync();

            totalOpenTasks = await _footballContext.Tasks
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Status, "open") && t.PlayerId == loggedInPlayerId)
                .CountAsync();

            var hasAnyReview = await _footballContext.Reviews
                .AsNoTracking()
                .AnyAsync(r => r.PlayerId == loggedInPlayerId);

            totalNeedsReview = hasAnyReview ? 0 : 1;
        }
        else
        {
            totalExpiringContracts = await _footballContext.Players1
                .AsNoTracking()
                .Where(p => p.ContractEndDate != null && p.ContractEndDate >= today && p.ContractEndDate <= cutoff)
                .CountAsync();

            totalOpenTasks = await _footballContext.Tasks
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Status, "open"))
                .CountAsync();

            totalNeedsReview = await _footballContext.Players1
                .AsNoTracking()
                .Where(p => !_footballContext.Reviews.Any(r => r.PlayerId == p.PlayerId))
                .CountAsync();
        }

        // Upcoming tasks: due today or later, ordered by due date, max 6
        var tasksQuery = _footballContext.Tasks
            .AsNoTracking()
            .Include(t => t.Player)
            .Include(t => t.Club)
            .Where(t => t.DueDate >= today);

        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            tasksQuery = tasksQuery.Where(t => t.PlayerId == loggedInPlayerId);
        }

        var rawUpcoming = await tasksQuery
            .OrderBy(t => t.DueDate)
            .Take(6)
            .Select(t => new
            {
                title = t.Title,
                description = t.Description,
                // assignedToId: prefer playerId if present, otherwise clubId
                assignedToId = t.PlayerId ?? t.ClubId,
                // assignedById: from the tasks table assigned_to_scout_id column
                assignedById = t.AssignedToScoutId,
                assigned_to = t.Player != null ? t.Player.FullName : (t.Club != null ? t.Club.ClubName : null),
                dueDate = t.DueDate,
                taskStatus = t.Status,
                source = t.Source,
                createdAt = t.CreatedAt,
                taskId = t.TaskId
            })
            .ToListAsync();

        // Resolve assigned_by: prefer scout name from stf.scouts; if not found, try identity user roles -> role name
        var assignedIds = rawUpcoming
            .Select(r => r.assignedById)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var scoutsMap = new Dictionary<string, string>();
        if (assignedIds.Count > 0)
        {
            scoutsMap = await _footballContext.Scouts
                .AsNoTracking()
                .Where(s => assignedIds.Contains(s.ScoutId))
                .ToDictionaryAsync(s => s.ScoutId, s => s.ScoutName);
        }

        var remainingIds = assignedIds.Where(id => !scoutsMap.ContainsKey(id)).ToList();
        var userRoleMap = new Dictionary<string, string>();
        if (remainingIds.Count > 0)
        {
            var userRoles = await _appDbContext.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
                .AsNoTracking()
                .Where(ur => remainingIds.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.RoleId })
                .ToListAsync();

            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
            var roles = new Dictionary<string, string>();
            if (roleIds.Count > 0)
            {
                roles = await _appDbContext.Roles
                    .AsNoTracking()
                    .Where(r => roleIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Name);
            }

            foreach (var ur in userRoles)
            {
                if (!userRoleMap.ContainsKey(ur.UserId))
                {
                    roles.TryGetValue(ur.RoleId, out var roleName);
                    userRoleMap[ur.UserId] = roleName; // may be null
                }
            }
        }

        var upcomingTasks = rawUpcoming.Select(r => new
        {
            title = r.title,
            description = r.description,
            assignedToId = r.assignedToId,
            assignedById = r.assignedById,
            assigned_by = string.IsNullOrWhiteSpace(r.assignedById)
                ? "Auto-generated"
                : (scoutsMap.TryGetValue(r.assignedById, out var sname)
                    ? sname
                    : (userRoleMap.TryGetValue(r.assignedById, out var roleName) ? roleName : null)),
            assigned_to = r.assigned_to,
            dueDate = r.dueDate,
            taskStatus = r.taskStatus,
            source = r.source,
            createdAt = r.createdAt,
            taskId = r.taskId
        }).ToList();


        // Recent notes: most recent first, max 5. Execute raw SQL (exact query provided) and map results.
        var recentNotes = new List<object>();

        string sql;
        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            sql = @"SELECT
    n.note_id AS note_id,
    CASE WHEN n.player_id IS NOT NULL THEN n.player_id ELSE n.club_id END AS noteForId,
    CASE WHEN p.full_name IS NOT NULL THEN p.full_name ELSE c.club_name END AS noteForName,
    CASE WHEN n.player_id IS NOT NULL THEN 'player' ELSE 'club' END AS playerOrClubNote,
    n.created_at AS noteCreatedAt,
    n.topic AS notesTopic,
    n.category AS noteCategory
FROM stf.notes n
LEFT JOIN stf.players p ON n.player_id = p.player_id
LEFT JOIN stf.clubs c ON n.club_id = c.club_id
WHERE n.player_id = @playerId AND n.is_visible_to_player = true
ORDER BY n.created_at DESC
LIMIT 5;";
        }
        else
        {
            sql = @"SELECT
    n.note_id AS note_id,
    CASE WHEN n.player_id IS NOT NULL THEN n.player_id ELSE n.club_id END AS noteForId,
    CASE WHEN p.full_name IS NOT NULL THEN p.full_name ELSE c.club_name END AS noteForName,
    CASE WHEN n.player_id IS NOT NULL THEN 'player' ELSE 'club' END AS playerOrClubNote,
    n.created_at AS noteCreatedAt,
    n.topic AS notesTopic,
    n.category AS noteCategory
FROM stf.notes n
LEFT JOIN stf.players p ON n.player_id = p.player_id
LEFT JOIN stf.clubs c ON n.club_id = c.club_id
ORDER BY n.created_at DESC
LIMIT 5;";
        }

        var conn = _footballContext.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Closed)
            await conn.OpenAsync();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@playerId";
                p.Value = loggedInPlayerId;
                cmd.Parameters.Add(p);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var noteId = reader.IsDBNull(0) ? null : reader.GetString(0);
                var noteForId = reader.IsDBNull(1) ? null : reader.GetString(1);
                var noteForName = reader.IsDBNull(2) ? null : reader.GetString(2);
                var noteCreatedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                var notesTopic = reader.IsDBNull(5) ? null : reader.GetString(5);
                var noteCategory = reader.IsDBNull(6) ? null : reader.GetString(6);
                var playerOrClubNote = reader.IsDBNull(3) ? null : reader.GetString(3);

                recentNotes.Add(new
                {
                    note_id = noteId,
                    noteForId = noteForId,
                    noteForName = noteForName,
                    noteCreatedAt = noteCreatedAt,
                    notesTopic = notesTopic,
                    noteCategory = noteCategory,
                    playerOrClubNote = playerOrClubNote
                });
            }
        }





        // Recent emails: latest 5 by sent_at
        // var recentEmails = await _footballContext.Emails
        //     .AsNoTracking()
        //     .Include(e => e.Player)
        //     .Include(e => e.Club)
        //     .Include(e => e.SentByScout)
        //     .OrderByDescending(e => e.SentAt)
        //     .Take(5)
        //     .Select(e => new
        //     {
        //         emailId = e.EmailId,
        //         playerId = e.PlayerId,
        //         playerName = e.Player != null ? e.Player.FullName : null,
        //         clubId = e.ClubId,
        //         clubName = e.Club != null ? e.Club.ClubName : null,
        //         recipientEmail = e.RecipientEmail,
        //         subject = e.Subject,
        //         sentByScoutId = e.SentByScoutId,
        //         scoutName = e.SentByScout != null ? e.SentByScout.ScoutName : null,
        //         sentAt = e.SentAt
        //     })
        //     .ToListAsync();

        // Recent emails: run raw SQL similar to recent notes and map results
        var recentEmails = new List<object>();

        string emailsSql;
        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            emailsSql = @"SELECT
    e.email_id AS emailId,
    e.player_id AS playerId,
    p.full_name AS playerName,
    e.club_id AS clubId,
    c.club_name AS clubName,
    e.recipient_email AS recipientEmail,
    e.subject AS subject,
    e.sent_by_scout_id AS sentByScoutId,
    s.scout_name AS scoutName,
    e.sent_at AS sentAt
FROM stf.emails e
LEFT JOIN stf.players p ON e.player_id = p.player_id
LEFT JOIN stf.clubs c ON e.club_id = c.club_id
LEFT JOIN stf.scouts s ON e.sent_by_scout_id = s.scout_id
WHERE e.player_id = @playerId
ORDER BY e.sent_at DESC
LIMIT 5;";
        }
        else
        {
            emailsSql = @"SELECT
    e.email_id AS emailId,
    e.player_id AS playerId,
    p.full_name AS playerName,
    e.club_id AS clubId,
    c.club_name AS clubName,
    e.recipient_email AS recipientEmail,
    e.subject AS subject,
    e.sent_by_scout_id AS sentByScoutId,
    s.scout_name AS scoutName,
    e.sent_at AS sentAt
FROM stf.emails e
LEFT JOIN stf.players p ON e.player_id = p.player_id
LEFT JOIN stf.clubs c ON e.club_id = c.club_id
LEFT JOIN stf.scouts s ON e.sent_by_scout_id = s.scout_id
ORDER BY e.sent_at DESC
LIMIT 5;";
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = emailsSql;
            if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@playerId";
                p.Value = loggedInPlayerId;
                cmd.Parameters.Add(p);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var emailId = reader.IsDBNull(0) ? null : reader.GetString(0);
                var playerId = reader.IsDBNull(1) ? null : reader.GetString(1);
                var playerName = reader.IsDBNull(2) ? null : reader.GetString(2);
                var clubId = reader.IsDBNull(3) ? null : reader.GetString(3);
                var clubName = reader.IsDBNull(4) ? null : reader.GetString(4);
                var recipientEmail = reader.IsDBNull(5) ? null : reader.GetString(5);
                var subject = reader.IsDBNull(6) ? null : reader.GetString(6);
                var sentByScoutId = reader.IsDBNull(7) ? null : reader.GetString(7);
                var scoutName = reader.IsDBNull(8) ? null : reader.GetString(8);
                var sentAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);

                recentEmails.Add(new
                {
                    email_id = emailId,
                    sendEmailForId = playerId ?? clubId,
                    sendEmailForName = playerName ?? clubName,
                    mailForClubOrPlayer = playerId != null ? "player" : (clubId != null ? "club" : null),
                    sentById = sentByScoutId,
                    sentByName = scoutName,
                    sentTo = recipientEmail,
                    sentAt = sentAt
                });
            }
        }






        // Recent reviewed players: latest 5 reviews with current and overall ratings
        var latestReviews = await _footballContext.Reviews
            .AsNoTracking()
            .Include(r => r.Player)
            .Include(r => r.Scout)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var latestReviewIds = latestReviews.Select(r => r.ReviewId).ToList();
        var latestPlayerIds = latestReviews.Select(r => r.PlayerId).Distinct().ToList();

        var currentRatingsByReview = await _footballContext.ReviewActivityRatings
            .AsNoTracking()
            .Where(ar => latestReviewIds.Contains(ar.ReviewId))
            .GroupBy(ar => ar.ReviewId)
            .Select(g => new { reviewId = g.Key, avg = g.Average(x => (double)x.Rating) })
            .ToDictionaryAsync(x => x.reviewId, x => (double?)x.avg);

        var overallRatingsByPlayer = await _footballContext.Reviews
            .AsNoTracking()
            .Where(rr => latestPlayerIds.Contains(rr.PlayerId))
            .Join(_footballContext.ReviewActivityRatings, rr => rr.ReviewId, rar => rar.ReviewId, (rr, rar) => new { rr.PlayerId, rar.Rating })
            .GroupBy(x => x.PlayerId)
            .Select(g => new { playerId = g.Key, avg = g.Average(x => (double)x.Rating) })
            .ToDictionaryAsync(x => x.playerId, x => (double?)x.avg);

        var recentlyReviewedPlayers = latestReviews.Select(r => new
        {
            reviewId = r.ReviewId,
            createdAt = r.CreatedAt,
            playerId = r.PlayerId,
            playerName = r.Player != null ? r.Player.FullName : null,
            playerPosition = r.Player != null ? r.Player.PositionCode : null,
            reviewedById = r.ScoutId,
            reviewedByName = r.Scout != null ? r.Scout.ScoutName : null,
            matchDate = r.MatchDate,
            overallRating = overallRatingsByPlayer.ContainsKey(r.PlayerId) ? overallRatingsByPlayer[r.PlayerId] : null,
            currentRating = currentRatingsByReview.ContainsKey(r.ReviewId) ? currentRatingsByReview[r.ReviewId] : null,
        }).ToList();

        // Contract alerts: any contract ending within next 2 months
        var nowUtc = DateTime.UtcNow;
        var cutoffDateTime = DateTime.UtcNow.AddMonths(2);

        var contractPlayersQuery = _footballContext.Players1
            .AsNoTracking()
            .Include(p => p.AgentScout)
            .Include(p => p.CurrentClub)
            .Where(p => (p.ContractEndDate != null && p.ContractEndDate >= today && p.ContractEndDate <= cutoff)
                        || (p.ContractEndWithCoach != null && p.ContractEndWithCoach >= today && p.ContractEndWithCoach <= cutoff)
                        || _footballContext.CommercialContracts.Any(cc => cc.PlayerId == p.PlayerId && cc.ContractEndDate >= nowUtc && cc.ContractEndDate <= cutoffDateTime)
            );

        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            contractPlayersQuery = contractPlayersQuery.Where(p => p.PlayerId == loggedInPlayerId);
        }

        var contractAlertPlayers = await contractPlayersQuery.ToListAsync();

        var contractPlayerIds = contractAlertPlayers.Select(p => p.PlayerId).ToList();

        var commercialMinByPlayer = await _footballContext.CommercialContracts
            .AsNoTracking()
            .Where(cc => contractPlayerIds.Contains(cc.PlayerId) && cc.ContractEndDate >= nowUtc && cc.ContractEndDate <= cutoffDateTime)
            .GroupBy(cc => cc.PlayerId)
            .Select(g => new { playerId = g.Key, minEnd = g.Min(x => x.ContractEndDate) })
            .ToDictionaryAsync(x => x.playerId, x => (DateTime?)x.minEnd);

        var contractAlerts = contractAlertPlayers.Select(p => new
        {
            playerId = p.PlayerId,
            playerName = p.FullName,
            playerPosition = p.PositionCode,
            scoutId = p.AgentScoutId,
            scoutName = p.AgentScout != null ? p.AgentScout.ScoutName : null,
            clubId = p.CurrentClubId,
            clubName = p.CurrentClub != null ? p.CurrentClub.ClubName : null,
            clubContractEndDate = p.ContractEndDate,
            scoutContractEndDate = p.ContractEndWithCoach,
            commercialContractEndDate = commercialMinByPlayer.ContainsKey(p.PlayerId) ? commercialMinByPlayer[p.PlayerId] : null
        }).ToList();

        // Upcoming review alerts from tasks: status=open, source=review, due_date within next 4 weeks
        var fourWeeksFromToday = today.AddDays(28);

        var reviewQuery = _footballContext.Tasks
            .AsNoTracking()
            .Include(t => t.Player)
                .ThenInclude(p => p.AgentScout)
            .Include(t => t.Player)
                .ThenInclude(p => p.CurrentClub)
            .Where(t => EF.Functions.ILike(t.Status, "open")
                        && EF.Functions.ILike(t.Source, "review")
                        && t.DueDate >= today
                        && t.DueDate <= fourWeeksFromToday
                        && t.PlayerId != null);

        if (!string.IsNullOrWhiteSpace(loggedInPlayerId))
        {
            reviewQuery = reviewQuery.Where(t => t.PlayerId == loggedInPlayerId);
        }

        var upcomingReviewAlerts = await reviewQuery
            .Select(t => new
            {
                reviewToId = t.PlayerId,
                reviewToName = t.Player != null ? t.Player.FullName : null,
                matchDate = t.DueDate,
                playerPosition = t.Player != null ? t.Player.PositionCode : null,
                scoutId = t.Player != null ? t.Player.AgentScoutId : null,
                scoutName = t.Player != null && t.Player.AgentScout != null ? t.Player.AgentScout.ScoutName : null,
                clubId = t.Player != null ? t.Player.CurrentClubId : null,
                clubName = t.Player != null && t.Player.CurrentClub != null ? t.Player.CurrentClub.ClubName : null,
                taskId = t.TaskId
            })
            .ToListAsync();

        // Global count: count rows in stf.contracts where end_date within next 2 months
        var expiringContractsCount = await _footballContext.Contracts
            .AsNoTracking()
            .Where(c => c.EndDate >= nowUtc && c.EndDate <= cutoffDateTime)
            .CountAsync();

        return Ok(new
        {
            dashboardCounters = new
            {
                dashboardTotalPlayers = totalPlayers,
                dashboardTotalExpiringContracts = expiringContractsCount,
                dashboardTotalOpenTasks = totalOpenTasks,
                dashboardNeedsReview = totalNeedsReview
            },
            dashboardUpcomingTasks = upcomingTasks,
            dashboardRecentNotes = recentNotes,
            dashboardRecentEmail = recentEmails,
            // recently reviewed players (backend + frontend aliases)
            dashboardRecentlyReviewedPlayers = recentlyReviewedPlayers,
            // contract alerts
            dashboardContractAlerts = contractAlerts,
            // upcoming review alerts (from tasks)
            dashboardUpcomingReviewAlerts = upcomingReviewAlerts,
        });
    }

    private async Task<object> BuildCompanyProfileAsync()
    {
        var companyProfile = await _companyProfileRepository.GetAsync();

        if (companyProfile == null)
        {
            return new
            {
                companyName = string.Empty,
                shortName = string.Empty,
                logoUrl = string.Empty,
                contractExpiringMonths = 6,
            };
        }

        return new
        {
            companyName = companyProfile.CompanyName,
            shortName = companyProfile.ShortName,
            logoUrl = companyProfile.LogoUrl,
            contractExpiringMonths = companyProfile.ContractExpiringMonths,
        };
    }
}
