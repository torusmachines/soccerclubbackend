using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using FootballDashboardAPI.Models.Entities;
using FootballDashboardAPI.Models.Responses;
using FootballDashboardAPI.Repositories.Interfaces;
using FootballDashboardAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly FootballContext _footballContext;
    private readonly AppDbContext _appDbContext;
    private readonly IPlayerPositionService _playerPositionService;

    public PlayerRepository(
        FootballContext footballContext,
        AppDbContext appDbContext,
        IPlayerPositionService playerPositionService)
    {
        _footballContext = footballContext;
        _appDbContext = appDbContext;
        _playerPositionService = playerPositionService;
    }

    // all players for player page with specfied data not all.
    public async Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardAsync()
    {
        return await GetPlayersDashboardCoreAsync(null, null, null, null, null);
    }

    public async Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardFilteredAsync(
        string? positionCode,
        string? scoutId,
        int? sportId,
        string? search,
        string? restrictToScoutId)
    {
        return await GetPlayersDashboardCoreAsync(positionCode, scoutId, sportId, search, restrictToScoutId);
    }

    private async Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardCoreAsync(
        string? positionCode,
        string? scoutId,
        int? sportId,
        string? search,
        string? restrictToScoutId)
    {
        var playersQuery = _footballContext.Players1
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(positionCode))
            playersQuery = playersQuery.Where(p => p.PositionCode == positionCode);

        if (sportId.HasValue)
            playersQuery = playersQuery.Where(p => p.SportId == sportId);

        if (!string.IsNullOrWhiteSpace(restrictToScoutId))
            playersQuery = playersQuery.Where(p => p.AgentScoutId == restrictToScoutId);
        else if (!string.IsNullOrWhiteSpace(scoutId))
            playersQuery = playersQuery.Where(p => p.AgentScoutId == scoutId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            playersQuery = playersQuery.Where(p =>
                EF.Functions.ILike(p.FullName, term) ||
                EF.Functions.ILike(p.Nationality ?? string.Empty, term) ||
                EF.Functions.ILike(p.PositionCode ?? string.Empty, term) ||
                _footballContext.Clubs.Any(c => c.ClubId == p.CurrentClubId && EF.Functions.ILike(c.ClubName, term)) ||
                _footballContext.Scouts.Any(s => s.ScoutId == p.AgentScoutId && EF.Functions.ILike(s.ScoutName, term)));
        }

        var playerRows = await playersQuery
            .OrderBy(p => p.FullName)
            .Select(p => new PlayerDashboardProjection
            {
                PlayerId = p.PlayerId,
                PlayerEmail = p.playerEmail,
                UserStatus = p.UserStatus,
                FullName = p.FullName,
                PositionCode = p.PositionCode,
                Nationality = p.Nationality,
                ContractStartDate = p.ContractStartDate,
                ContractEndDate = p.ContractEndDate,
                ContractEndWithCoach = p.ContractEndWithCoach,
                ClubName = p.CurrentClub != null ? p.CurrentClub.ClubName : string.Empty,
                ScoutId = p.AgentScoutId,
                ScoutName = p.AgentScout != null ? p.AgentScout.ScoutName : null,
                // include profile image URL from players table
                ProfileImageUrl = p.ProfileImageUrl,
                SportId = p.SportId,
                SportName = p.Sport != null ? p.Sport.SportName : null
            })
            .ToListAsync();

        var playerIds = playerRows.Select(p => p.PlayerId).ToList();
        var playerEmails = playerRows
            .Select(p => NormalizeEmail(p.PlayerEmail))
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        var ratingsByPlayer = await GetOverallRatingsMapAsync(playerIds);
        var userByEmail = await GetUsersByEmailMapAsync(playerEmails);

        return playerRows.Select(p =>
        {
            var normalizedEmail = NormalizeEmail(p.PlayerEmail);
            userByEmail.TryGetValue(normalizedEmail, out var user);

            return new PlayerListResponse
            {
                PlayerId = p.PlayerId,
                UserId = user?.Id,
                PlayerEmail = p.PlayerEmail,
                //  UserStatus = user?.UserStatus ?? p.UserStatus ?? "Pending",
                UserStatus = p.UserStatus,
                PlayerName = p.FullName,
                ClubName = p.ClubName ?? string.Empty,
                Position = p.PositionCode ?? string.Empty,
                Nationality = p.Nationality ?? string.Empty,
                ContractStartDate = p.ContractStartDate,
                ContractEndDate = p.ContractEndDate,
                OverallRating = ratingsByPlayer.TryGetValue(p.PlayerId, out var rating)
                    ? rating
                    : 0m,
                AgencyContractStatus = GetAgencyContractStatus(p.ContractEndWithCoach),
                ScoutId = p.ScoutId,
                ScoutName = p.ScoutName,
                PlayerProfileImage = p.ProfileImageUrl,
                SportId = p.SportId,
                SportName = p.SportName
            };
        }).ToList();
    }

    private async Task<Dictionary<string, decimal>> GetOverallRatingsMapAsync(List<string> playerIds)
    {
        if (playerIds.Count == 0)
            return new Dictionary<string, decimal>();

        return await (
            from r in _footballContext.Reviews.AsNoTracking()
            join rar in _footballContext.ReviewActivityRatings.AsNoTracking() on r.ReviewId equals rar.ReviewId
            where playerIds.Contains(r.PlayerId)
            group rar by r.PlayerId into g
            select new
            {
                PlayerId = g.Key,
                Overall = Math.Round(g.Average(x => x.Rating), 2)
            })
            .ToDictionaryAsync(x => x.PlayerId, x => x.Overall);
    }

    private async Task<Dictionary<string, AppUserProjection>> GetUsersByEmailMapAsync(List<string> emails)
    {
        if (emails.Count == 0)
            return new Dictionary<string, AppUserProjection>();

        var users = await _appDbContext.Users
            .AsNoTracking()
            .Where(u => u.Email != null)
            .Select(u => new AppUserProjection
            {
                Id = u.Id,
                Email = u.Email!,
                UserStatus = u.UserStatus
            })
            .ToListAsync();

        return users
            .Where(u => emails.Contains(NormalizeEmail(u.Email)))
            .GroupBy(u => NormalizeEmail(u.Email))
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    // coach contract based conracr status
    private static string GetAgencyContractStatus(DateOnly? contractEndDate)
    {
        if (!contractEndDate.HasValue)
            return "Available";

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (contractEndDate.Value < today)
            return "Available";

        if (contractEndDate.Value <= today.AddMonths(1))
            return "Expiring Soon";

        return "Active";
    }

    //PlayerDetails service For Overview tab
    public async Task<PlayerDetailsResponse?> GetPlayerDetailsAsync(string playerId)
    {
        var player = await _footballContext.Players1
            .AsNoTracking()
            .Include(p => p.Sport)
            .Include(p => p.AgentScout)
            .Include(p => p.CurrentClub)
            .FirstOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
            return null;

        var allRatings = await (
            from r in _footballContext.Reviews.AsNoTracking()
            join rar in _footballContext.ReviewActivityRatings.AsNoTracking() on r.ReviewId equals rar.ReviewId
            where r.PlayerId == playerId
            select rar.Rating)
            .ToListAsync();

        var overallRating = allRatings.Count == 0
            ? 0m
            : Math.Round(allRatings.Average(), 2);

        var activityRatings = await (
            from r in _footballContext.Reviews.AsNoTracking()
            join rar in _footballContext.ReviewActivityRatings.AsNoTracking() on r.ReviewId equals rar.ReviewId
            join sa in _footballContext.SportActivities.AsNoTracking() on rar.ActivityId equals sa.ActivityId
            where r.PlayerId == playerId
            group rar by new { sa.ActivityId, sa.ActivityName } into g
            orderby g.Key.ActivityName
            select new ActivityRatingResponse
            {
                ActivityId = g.Key.ActivityId,
                ActivityName = g.Key.ActivityName,
                AverageRating = Math.Round(g.Average(x => x.Rating), 2)
            })
            .ToListAsync();

        var response = new PlayerDetailsResponse
        {
            PlayerId = player.PlayerId,
            PlayerName = player.FullName,
            DateOfBirth = player.DateOfBirth,
            Nationality = player.Nationality ?? string.Empty,
            Position = player.PositionCode ?? string.Empty,
            PreferredFoot = player.PreferredFoot ?? string.Empty,
            HeightCm = player.HeightCm,
            WeightKg = player.WeightKg,
            ContactInfo = player.ContactInfo ?? string.Empty,
            AgentName = player.AgentName ?? string.Empty,
            ScoutId = player.AgentScoutId,
            ScoutName = player.AgentScout?.ScoutName,
            ContractStartDate = player.ContractStartDate,
            ContractEndDate = player.ContractEndDate,
            ContractStartWithCoach = player.ContractStartWithCoach,
            ContractEndWithCoach = player.ContractEndWithCoach,
            ContractStatus = GetAgencyContractStatus(player.ContractEndWithCoach),
            SportId = player.SportId,
            SportName = player.Sport?.SportName ?? string.Empty,
            CurrentClubId = player.CurrentClubId,
            CurrentClubName = player.CurrentClub?.ClubName,
            // map extended profile fields from DB model
            PlayerEmail = player.playerEmail ?? string.Empty,
            Gender = player.Gender,
            PlaceOfBirth = player.PlaceOfBirth,
            PrimaryLanguage = player.PrimaryLanguage,
            SecondaryLanguage = player.SecondaryLanguage,
            ProfileVisibility = player.ProfileVisibility,
            ProfileImageUrl = player.ProfileImageUrl,
            PhoneNumber = player.PhoneNumber,
            AlternatePhone = player.AlternatePhone,
            EmergencyContactName = player.EmergencyContactName,
            EmergencyContactNumber = player.EmergencyContactNumber,
            AddressLine1 = player.AddressLine1,
            AddressLine2 = player.AddressLine2,
            City = player.City,
            State = player.State,
            Country = player.Country,
            PostalCode = player.PostalCode,
            SecondaryPosition = player.SecondaryPosition,
            JerseyNumber = player.JerseyNumber,
            ExperienceYears = player.ExperienceYears,
            PlayingLevel = player.PlayingLevel,
            DominantSide = player.DominantSide,
            FitnessLevel = player.FitnessLevel,
            InjuryStatus = player.InjuryStatus,
            CoachEmail = player.CoachEmail,
            CoachPhone = player.CoachPhone,
            OverallRating = overallRating,
            ActivityRatings = activityRatings
        };

        response.player_sport_details = await GetPlayerSportDetailsAsync(response.SportId, response.SportName);
        response.player_all_review = (await GetPlayerReviewsAsync(playerId)).ToList();
        response.player_all_notes = (await GetPlayerNotesAsync(playerId)).ToList();
        response.player_all_documents = (await GetPlayerDocumentsAsync(playerId)).ToList();
        response.player_all_tasks = await GetPlayerTasksAsync(playerId);
        response.player_all_emails = await GetPlayerEmailsAsync(playerId);
        response.player_all_commercial_contracts = await GetPlayerCommercialContractsAsync(playerId);
        response.playerDetailsOtherData = await BuildPlayerDetailsOtherDataAsync();

        return response;
    }

    private async Task<PlayerDetailsOtherDataResponse> BuildPlayerDetailsOtherDataAsync()
    {
        var positions = (await _playerPositionService.GetAllAsync()).ToList();

        // Fallback: if SportId is missing on any position, try to re-fetch that position individually
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            if (!p.SportId.HasValue)
            {
                try
                {
                    var full = await _playerPositionService.GetByIdAsync(p.PositionId);
                    if (full?.SportId.HasValue == true)
                    {
                        positions[i].SportId = full.SportId;
                    }
                }
                catch
                {
                    // ignore failures and keep SportId as null
                }
            }
        }

        var playerDetailsPositionData = positions
            .OrderBy(p => p.PositionName)
            .Select(p => new PlayerDetailsPositionOptionResponse
            {
                positionId = p.PositionId,
                positionName = p.PositionName,
                positionCode = p.PositionCode,
                positionForSportId = p.SportId,
            })
            .ToList();

        var playerDetailsSportsData = await _footballContext.Sports
            .AsNoTracking()
            .OrderBy(s => s.SportName)
            .Select(s => new PlayerDetailsSportOptionResponse
            {
                sportId = s.SportId,
                sportName = s.SportName,
            })
            .ToListAsync();

        var playerDetailsScoutData = await _footballContext.Scouts
            .AsNoTracking()
            .OrderBy(s => s.ScoutName)
            .Select(s => new PlayerDetailsScoutOptionResponse
            {
                scoutId = s.ScoutId,
                scoutName = s.ScoutName,
            })
            .ToListAsync();

        var playerDetailsClubData = await _footballContext.Clubs
            .AsNoTracking()
            .OrderBy(c => c.ClubName)
            .Select(c => new PlayerDetailsClubOptionResponse
            {
                clubId = c.ClubId,
                clubName = c.ClubName,
            })
            .ToListAsync();

        var playerDetailsTemplate = await _footballContext.Templates
            .AsNoTracking()
            .OrderBy(t => t.TemplateName)
            .Select(t => new PlayerDetailsTemplateResponse
            {
                templateId = t.TemplateId,
                templateName = t.TemplateName,
                templateType = t.TemplateType,
                subject = t.Subject ?? string.Empty,
                body = t.Body ?? string.Empty,
            })
            .ToListAsync();

        return new PlayerDetailsOtherDataResponse
        {
            playerDetailsPositionData = playerDetailsPositionData,
            playerDetailsSportsData = playerDetailsSportsData,
            playerDetailsScoutData = playerDetailsScoutData,
            playerDetailsClubData = playerDetailsClubData,
            playerDetailsTemplate = playerDetailsTemplate,
        };
    }

    //player id based all review and its details.
    public async Task<IEnumerable<PlayerReviewResponse>> GetPlayerReviewsAsync(string playerId)
    {
        var rows = await (
            from r in _footballContext.Reviews.AsNoTracking()
            join rar in _footballContext.ReviewActivityRatings.AsNoTracking() on r.ReviewId equals rar.ReviewId
            join sa in _footballContext.SportActivities.AsNoTracking() on rar.ActivityId equals sa.ActivityId
            where r.PlayerId == playerId
            orderby r.MatchDate descending, sa.ActivityName
            select new
            {
                r.ReviewId,
                r.ScoutId,
                ScoutName = r.Scout.ScoutName,
                r.MatchDate,
                Club1Name = r.Club1 != null ? r.Club1.ClubName : string.Empty,
                Club2Name = r.Club2 != null ? r.Club2.ClubName : string.Empty,
                Notes = r.Notes ?? string.Empty,
                sa.ActivityId,
                sa.ActivityName,
                rar.Rating,
                Comment = rar.Comment ?? string.Empty
            })
            .ToListAsync();

        var reviews = rows
            .GroupBy(x => new
            {
                x.ReviewId,
                x.ScoutId,
                x.ScoutName,
                x.MatchDate,
                x.Club1Name,
                x.Club2Name,
                x.Notes
            })
            .Select(g => new PlayerReviewResponse
            {
                ReviewId = g.Key.ReviewId,
                ScoutId = g.Key.ScoutId,
                ScoutName = g.Key.ScoutName,
                MatchDate = g.Key.MatchDate,
                Club1Name = g.Key.Club1Name,
                Club2Name = g.Key.Club2Name,
                Notes = g.Key.Notes,
                AverageRating = Math.Round(g.Average(x => x.Rating), 2),
                Activities = g.Select(x => new ReviewActivityResponse
                {
                    ActivityId = x.ActivityId,
                    ActivityName = x.ActivityName,
                    Rating = x.Rating,
                    Comment = x.Comment
                }).ToList()
            })
            .ToList();

        return reviews;
    }

    private async Task<PlayerSportDetailsResponse> GetPlayerSportDetailsAsync(int? sportId, string? sportName)
    {
        if (!sportId.HasValue)
        {
            return new PlayerSportDetailsResponse
            {
                sport_name = sportName ?? string.Empty,
                sport_id = null,
                sport_entity = new List<PlayerSportEntityResponse>()
            };
        }

        var entities = await _footballContext.SportActivities
            .AsNoTracking()
            .Where(sa => sa.SportId == sportId.Value)
            .OrderBy(sa => sa.ActivityName)
            .Select(sa => new PlayerSportEntityResponse
            {
                entity_id = sa.ActivityId,
                entity_name = sa.ActivityName
            })
            .ToListAsync();

        return new PlayerSportDetailsResponse
        {
            sport_name = sportName ?? string.Empty,
            sport_id = sportId,
            sport_entity = entities
        };
    }

    //player all notes
    private async Task<List<PlayerNoteResponse>> GetPlayerNotesAsync(string playerId)
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new PlayerNoteResponse
            {
                NoteId = n.NoteId,
                Topic = n.Topic ?? string.Empty,
                Description = n.Description ?? string.Empty,
                Category = n.Category ?? string.Empty,
                FollowUpDate = n.FollowUpDate,
                CreatedByScoutId = n.CreatedByScoutId ?? string.Empty,
                CreatedAt = n.CreatedAt,
                IsVisibleToPlayer = n.IsVisibleToPlayer
            })
            .ToListAsync();
    }

    //player all documents
    private async Task<List<PlayerDocumentResponse>> GetPlayerDocumentsAsync(string playerId)
    {
        return await _footballContext.Documents
            .AsNoTracking()
            .Where(d => d.PlayerId == playerId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new PlayerDocumentResponse
            {
                DocumentId = d.DocumentId,
                DocumentName = d.DocumentName ?? string.Empty,
                DocumentType = d.DocumentType ?? string.Empty,
                DocumentDate = d.DocumentDate,
                FileSizeLabel = d.FileSizeLabel ?? string.Empty,
                FileExtension = string.Empty,
                CreatedAt = d.CreatedAt,
                IsVisibleToPlayer = d.IsVisibleToPlayer
            })
            .ToListAsync();
    }

    //player all task
    private async Task<List<PlayerTaskResponse>> GetPlayerTasksAsync(string playerId)
    {
        return await _footballContext.Tasks
            .AsNoTracking()
            .Where(t => t.PlayerId == playerId)
            .OrderBy(t => t.DueDate)
            .Select(t => new PlayerTaskResponse
            {
                TaskId = t.TaskId,
                Title = t.Title ?? string.Empty,
                Description = t.Description ?? string.Empty,
                DueDate = t.DueDate,
                Status = t.Status ?? string.Empty,
                Source = t.Source ?? string.Empty,
                AssignedToScoutId = t.AssignedToScoutId ?? string.Empty,
                TaskAssignedToPlayer = t.Player != null ? t.Player.FullName : string.Empty,
                AssignedToID = t.AssignedToScoutId,
                AssignedToName = string.IsNullOrWhiteSpace(t.AssignedToScoutId)
                    ? "Auto-generated"
                    : _footballContext.Scouts
                        .Where(s => s.ScoutId == t.AssignedToScoutId)
                        .Select(s => s.ScoutName)
                        .FirstOrDefault(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    //player emails
    private async Task<List<PlayerEmailResponse>> GetPlayerEmailsAsync(string playerId)
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.SentAt)
            .Select(e => new PlayerEmailResponse
            {
                EmailId = e.EmailId,
                RecipientEmail = e.RecipientEmail ?? string.Empty,
                Subject = e.Subject ?? string.Empty,
                Body = e.Body ?? string.Empty,
                SentByScoutId = e.SentByScoutId ?? string.Empty,
                SentAt = e.SentAt
            })
            .ToListAsync();
    }

    //player all commercial contracts
    private async Task<List<PlayerCommercialContractResponse>> GetPlayerCommercialContractsAsync(string playerId)
    {
        var playerName = await _footballContext.Players1
            .AsNoTracking()
            .Where(p => p.PlayerId == playerId)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return await _appDbContext.CommercialContracts
            .AsNoTracking()
            .Include(cc => cc.Sponsor)
            .Where(cc => cc.PlayerId == playerId)
            .OrderByDescending(cc => cc.ContractStartDate)
            .Select(cc => new PlayerCommercialContractResponse
            {
                Id = cc.Id,
                SponsorId = cc.SponsorId,
                SponsorCompanyName = cc.Sponsor != null ? cc.Sponsor.CompanyName : string.Empty,
                PlayerId = cc.PlayerId ?? string.Empty,
                PlayerName = playerName,
                EntityType = cc.EntityType ?? string.Empty,
                ClubId = cc.ClubId ?? string.Empty,
                ContractStartDate = cc.ContractStartDate,
                ContractEndDate = cc.ContractEndDate,
                ExpiryDate = cc.ExpiryDate,
                ContractStatus = GetCommercialContractStatus(cc.ContractEndDate),
                ContractDetails = cc.ContractDetails ?? string.Empty,
                DocumentPath = cc.DocumentPath ?? string.Empty,
                CreatedAt = cc.CreatedAt,
                UpdatedAt = cc.UpdatedAt
            })
            .ToListAsync();
    }

    private static string GetCommercialContractStatus(DateTime? contractEndDate)
    {
        if (!contractEndDate.HasValue)
            return "No Expiry";

        var today = DateTime.UtcNow.Date;

        if (contractEndDate.Value.Date < today)
            return "Expired";

        if (contractEndDate.Value.Date <= today.AddMonths(1))
            return "Expiring Soon";

        return "Active";
    }

    public async Task<FootballDashboardAPI.Models.Entities.Player?> GetByIdAsync(string id)
    {
        var player = await _footballContext.Players1
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == id);

        return player == null ? null : ToEntityPlayer(player);
    }

    public async Task<FootballDashboardAPI.Models.Entities.Player> CreateAsync(FootballDashboardAPI.Models.Entities.Player player)
    {
        var now = DateTime.UtcNow;
        player.PlayerId = Guid.NewGuid().ToString();
        player.CreatedAt = now;
        player.UpdatedAt = now;

        var model = new Player1
        {
            PlayerId = player.PlayerId,
            FullName = player.FullName,
            DateOfBirth = player.DateOfBirth ?? DateOnly.FromDateTime(now),
            Nationality = player.Nationality ?? string.Empty,
            PositionCode = player.PositionCode ?? string.Empty,
            PreferredFoot = player.PreferredFoot ?? string.Empty,
            HeightCm = player.HeightCm ?? 0,
            WeightKg = player.WeightKg ?? 0,
            CurrentClubId = player.CurrentClubId,
            ContractStartDate = player.ContractStartDate ?? DateOnly.FromDateTime(now),
            ContractEndDate = player.ContractEndDate ?? DateOnly.FromDateTime(now),
            ContractStartWithCoach = player.ContractStartWithCoach,
            ContractEndWithCoach = player.ContractEndWithCoach,
            AgentName = player.AgentName ?? string.Empty,
            AgentScoutId = player.AgentScoutId ?? string.Empty,
            ContactInfo = player.ContactInfo,
            // extended fields
            AddressLine1 = player.AddressLine1,
            AddressLine2 = player.AddressLine2,
            City = player.City,
            State = player.State,
            Country = player.Country,
            PostalCode = player.PostalCode,
            Gender = player.Gender,
            PlaceOfBirth = player.PlaceOfBirth,
            PrimaryLanguage = player.PrimaryLanguage,
            SecondaryLanguage = player.SecondaryLanguage,
            ProfileVisibility = player.ProfileVisibility,
            PhoneNumber = player.PhoneNumber,
            AlternatePhone = player.AlternatePhone,
            EmergencyContactName = player.EmergencyContactName,
            EmergencyContactNumber = player.EmergencyContactNumber,
            SecondaryPosition = player.SecondaryPosition,
            JerseyNumber = player.JerseyNumber,
            ExperienceYears = player.ExperienceYears,
            PlayingLevel = player.PlayingLevel,
            DominantSide = player.DominantSide,
            FitnessLevel = player.FitnessLevel,
            InjuryStatus = player.InjuryStatus,
            CoachEmail = player.CoachEmail,
            CoachPhone = player.CoachPhone,
            ProfileImageUrl = player.ProfileImageUrl,
            SportId = player.SportId,
            playerEmail = player.PlayerEmail ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            UserStatus = "Approved"
        };

        _footballContext.Players1.Add(model);
        await _footballContext.SaveChangesAsync();

        return player;
    }

    public async Task<FootballDashboardAPI.Models.Entities.Player?> UpdateAsync(FootballDashboardAPI.Models.Entities.Player player)
    {
        var existing = await _footballContext.Players1
            .FirstOrDefaultAsync(p => p.PlayerId == player.PlayerId);

        if (existing == null)
            return null;

        existing.FullName = player.FullName;
        existing.DateOfBirth = player.DateOfBirth ?? existing.DateOfBirth;
        existing.Nationality = player.Nationality ?? string.Empty;
        existing.PositionCode = player.PositionCode ?? string.Empty;
        existing.PreferredFoot = player.PreferredFoot ?? string.Empty;
        existing.HeightCm = player.HeightCm ?? 0;
        existing.WeightKg = player.WeightKg ?? 0;
        existing.CurrentClubId = player.CurrentClubId;
        existing.ContractStartDate = player.ContractStartDate ?? existing.ContractStartDate;
        existing.ContractEndDate = player.ContractEndDate ?? existing.ContractEndDate;
        existing.ContractStartWithCoach = player.ContractStartWithCoach;
        existing.ContractEndWithCoach = player.ContractEndWithCoach;
        existing.AgentName = player.AgentName ?? string.Empty;
        existing.AgentScoutId = player.AgentScoutId ?? string.Empty;
        existing.ContactInfo = player.ContactInfo;
        // persist extended fields
        existing.AddressLine1 = player.AddressLine1;
        existing.AddressLine2 = player.AddressLine2;
        existing.City = player.City;
        existing.State = player.State;
        existing.Country = player.Country;
        existing.PostalCode = player.PostalCode;
        existing.Gender = player.Gender;
        existing.PlaceOfBirth = player.PlaceOfBirth;
        existing.PrimaryLanguage = player.PrimaryLanguage;
        existing.SecondaryLanguage = player.SecondaryLanguage;
        existing.ProfileVisibility = player.ProfileVisibility;
        existing.PhoneNumber = player.PhoneNumber;
        existing.AlternatePhone = player.AlternatePhone;
        existing.EmergencyContactName = player.EmergencyContactName;
        existing.EmergencyContactNumber = player.EmergencyContactNumber;
        existing.SecondaryPosition = player.SecondaryPosition;
        existing.JerseyNumber = player.JerseyNumber ?? existing.JerseyNumber;
        existing.ExperienceYears = player.ExperienceYears ?? existing.ExperienceYears;
        existing.PlayingLevel = player.PlayingLevel;
        existing.DominantSide = player.DominantSide;
        existing.FitnessLevel = player.FitnessLevel;
        existing.InjuryStatus = player.InjuryStatus;
        existing.CoachEmail = player.CoachEmail;
        existing.CoachPhone = player.CoachPhone;
        existing.SportId = player.SportId;
        existing.playerEmail = player.PlayerEmail ?? string.Empty;
        existing.UpdatedAt = DateTime.UtcNow;

        await _footballContext.SaveChangesAsync();

        return player;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Players1
            .FirstOrDefaultAsync(p => p.PlayerId == id);

        if (existing == null)
            return false;

        _footballContext.Players1.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }

    private static FootballDashboardAPI.Models.Entities.Player ToEntityPlayer(Player1 player)
    {
        return new FootballDashboardAPI.Models.Entities.Player
        {
            PlayerId = player.PlayerId,
            FullName = player.FullName,
            DateOfBirth = player.DateOfBirth,
            Nationality = player.Nationality,
            PositionCode = player.PositionCode,
            PreferredFoot = player.PreferredFoot,
            HeightCm = player.HeightCm,
            WeightKg = player.WeightKg,
            CurrentClubId = player.CurrentClubId,
            ContractStartDate = player.ContractStartDate,
            ContractEndDate = player.ContractEndDate,
            ContractStartWithCoach = player.ContractStartWithCoach,
            ContractEndWithCoach = player.ContractEndWithCoach,
            AgentName = player.AgentName,
            PlayerEmail = player.playerEmail,
            AgentScoutId = player.AgentScoutId,
            ContactInfo = player.ContactInfo,
            ProfileImageUrl = player.ProfileImageUrl,
            SportId = player.SportId,
            // extended fields
            AddressLine1 = player.AddressLine1,
            AddressLine2 = player.AddressLine2,
            City = player.City,
            State = player.State,
            Country = player.Country,
            PostalCode = player.PostalCode,
            Gender = player.Gender,
            PlaceOfBirth = player.PlaceOfBirth,
            PrimaryLanguage = player.PrimaryLanguage,
            SecondaryLanguage = player.SecondaryLanguage,
            ProfileVisibility = player.ProfileVisibility,
            PhoneNumber = player.PhoneNumber,
            AlternatePhone = player.AlternatePhone,
            EmergencyContactName = player.EmergencyContactName,
            EmergencyContactNumber = player.EmergencyContactNumber,
            SecondaryPosition = player.SecondaryPosition,
            JerseyNumber = player.JerseyNumber,
            ExperienceYears = player.ExperienceYears,
            PlayingLevel = player.PlayingLevel,
            DominantSide = player.DominantSide,
            FitnessLevel = player.FitnessLevel,
            InjuryStatus = player.InjuryStatus,
            CoachEmail = player.CoachEmail,
            CoachPhone = player.CoachPhone,
            CreatedAt = player.CreatedAt,
            UpdatedAt = player.UpdatedAt
        };
    }

    private sealed class PlayerDashboardProjection
    {
        public string PlayerId { get; set; } = string.Empty;
        public string? PlayerEmail { get; set; }
        public string? UserStatus { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PositionCode { get; set; }
        public string? Nationality { get; set; }
        public DateOnly? ContractStartDate { get; set; }
        public DateOnly? ContractEndDate { get; set; }
        public DateOnly? ContractEndWithCoach { get; set; }
        public string? ClubName { get; set; }
        public string? ScoutId { get; set; }
        public string? ScoutName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int? SportId { get; set; }
        public string? SportName { get; set; }
    }

    private sealed class AppUserProjection
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserStatus { get; set; } = string.Empty;
    }
}
