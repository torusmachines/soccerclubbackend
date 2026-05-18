using FootballDashboardAPI.Models;
using FootballDashboardAPI.Models.Entities;
using FootballDashboardAPI.Models.Requests;
using FootballDashboardAPI.Models.Responses;
using FootballDashboardAPI.Services;
using FootballDashboardAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using PlayerEntity = FootballDashboardAPI.Models.Entities.Player;

namespace FootballDashboardAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IPlayerPositionService _playerPositionService;
    private readonly FootballContext _footballContext;

    private sealed class PlayerAccessScope
    {
        public bool IsAdmin { get; init; }
        public bool IsScoutOrCoach { get; init; }
        public string? LoggedInScoutId { get; init; }
        public string? RestrictToScoutId { get; init; }
        public string? LoggedInPlayerId { get; init; }
    }

    public PlayersController(
        IPlayerService playerService,
        IPlayerPositionService playerPositionService,
        FootballContext footballContext)
    {
        _playerService = playerService;
        _playerPositionService = playerPositionService;
        _footballContext = footballContext;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    [HttpGet]
    public async Task<ActionResult<PlayersListApiResponse>> GetPlayers(
        [FromQuery(Name = "positionCode")] string? positionCode,
        [FromQuery(Name = "scoutId")] string? scoutId,
        [FromQuery(Name = "sportId")] int? sportId,
        [FromQuery(Name = "search")] string? search)
    {
        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        if (access.IsScoutOrCoach && string.IsNullOrWhiteSpace(access.LoggedInScoutId))
        {
            return Ok(new PlayersListApiResponse
            {
                Players = Enumerable.Empty<PlayerListResponse>(),
                OtherData = await BuildOtherDataAsync(access)
            });
        }

        var players = await _playerService.GetPlayersDashboardFilteredAsync(
            positionCode,
            scoutId,
            sportId,
            search,
            access.RestrictToScoutId);

        return Ok(new PlayersListApiResponse
        {
            Players = players,
            OtherData = await BuildOtherDataAsync(access)
        });
    }

    [HttpGet("{playerId}")]
    public async Task<ActionResult<PlayerDetailsResponse>> GetPlayerDetails(string playerId)
    {
        if (!Guid.TryParse(playerId, out _))
        {
            return BadRequest(new { message = "playerId must be a valid GUID." });
        }

        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        var player = await _playerService.GetPlayerDetailsAsync(playerId);

        if (player == null)
        {
            return NotFound(new
            {
                message = $"Player with ID '{playerId}' not found."
            });
        }

        if (!string.IsNullOrWhiteSpace(access.RestrictToScoutId)
            && !string.Equals(player.ScoutId, access.RestrictToScoutId, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // If logged-in user is a Player, ensure they can only fetch their own player details
        if (!string.IsNullOrWhiteSpace(access.LoggedInPlayerId))
        {
            if (!string.Equals(player.PlayerId, access.LoggedInPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                // Hide notes and documents for other players by returning limited response
                player.player_all_notes = new List<PlayerNoteResponse>();
                player.player_all_documents = new List<PlayerDocumentResponse>();
                player.player_all_tasks = new List<PlayerTaskResponse>();
                player.player_all_emails = new List<PlayerEmailResponse>();
                // Still return the basic player overview
                return Ok(player);
            }
            else
            {
                // If the player is fetching their own profile, only include notes visible to player
                player.player_all_notes = player.player_all_notes?.Where(n => n.IsVisibleToPlayer).ToList() ?? new List<PlayerNoteResponse>();
                player.player_all_documents = player.player_all_documents?.Where(d => d.IsVisibleToPlayer).ToList() ?? new List<PlayerDocumentResponse>();
            }
        }

        return Ok(player);
    }

    [HttpGet("simplified")]
    public async Task<ActionResult<IEnumerable<PlayerSimplifiedResponse>>> GetPlayersSimplified(
        [FromQuery(Name = "sportId")] int? sportId)
    {
        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        var query = _footballContext.Players1
            .AsNoTracking()
            .Include(p => p.Sport)
            .Where(p => p.UserStatus == "Approved");

        if (!string.IsNullOrWhiteSpace(access.RestrictToScoutId))
        {
            query = query.Where(p => p.AgentScoutId == access.RestrictToScoutId);
        }

        if (sportId.HasValue)
        {
            query = query.Where(p => p.SportId == sportId);
        }

        var players = await query
            .Select(p => new PlayerSimplifiedResponse
            {
                PlayerId = p.PlayerId,
                PlayerName = p.FullName,
                SportId = p.SportId,
                SportName = p.Sport != null ? p.Sport.SportName : null
            })
            .OrderBy(p => p.PlayerName)
            .ToListAsync();

        return Ok(players);
    }

    [HttpPost]
    public async Task<ActionResult<PlayerEntity>> CreatePlayer([FromBody] CreatePlayerRequest request)
    {
        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(access.RestrictToScoutId))
        {
            request.AgentScoutId = access.RestrictToScoutId;
        }

        var player = new PlayerEntity
        {
            FullName = Truncate(request.FullName, 150),
            DateOfBirth = string.IsNullOrEmpty(request.DateOfBirth) ? null : DateOnly.Parse(request.DateOfBirth),
            Nationality = Truncate(request.Nationality, 100),
            PositionCode = Truncate(request.Position, 10),
            PreferredFoot = Truncate(request.PreferredFoot, 10),
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            CurrentClubId = Truncate(request.CurrentClub, 50),
            ContractStartDate = string.IsNullOrEmpty(request.ContractStart) ? null : DateOnly.Parse(request.ContractStart),
            ContractEndDate = string.IsNullOrEmpty(request.ContractEnd) ? null : DateOnly.Parse(request.ContractEnd),
            ContractStartWithCoach = string.IsNullOrEmpty(request.ContractStartWithCoach) ? null : DateOnly.Parse(request.ContractStartWithCoach),
            ContractEndWithCoach = string.IsNullOrEmpty(request.ContractEndWithCoach) ? null : DateOnly.Parse(request.ContractEndWithCoach),
            AgentName = Truncate(request.AgentName, 150),
            AgentScoutId = Truncate(request.AgentScoutId, 50),
            ContactInfo = Truncate(request.ContactInfo, 255),
            PlayerEmail = Truncate(request.PlayerEmail, 255),
            SportId = request.SportId,
            // extended fields
            Gender = Truncate(request.Gender, 10),
            PlaceOfBirth = Truncate(request.PlaceOfBirth, 100),
            PrimaryLanguage = Truncate(request.PrimaryLanguage, 50),
            SecondaryLanguage = Truncate(request.SecondaryLanguage, 50),
            ProfileVisibility = request.ProfileVisibility,
            PhoneNumber = Truncate(request.PhoneNumber, 20),
            AlternatePhone = Truncate(request.AlternatePhone, 20),
            EmergencyContactName = Truncate(request.EmergencyContactName, 100),
            EmergencyContactNumber = Truncate(request.EmergencyContactNumber, 20),
            AddressLine1 = Truncate(request.AddressLine1, 150),
            AddressLine2 = Truncate(request.AddressLine2, 150),
            City = Truncate(request.City, 100),
            State = Truncate(request.State, 100),
            Country = Truncate(request.Country, 100),
            PostalCode = Truncate(request.PostalCode, 20),
            SecondaryPosition = Truncate(request.SecondaryPosition, 50),
            JerseyNumber = request.JerseyNumber,
            ExperienceYears = request.ExperienceYears,
            PlayingLevel = Truncate(request.PlayingLevel, 20),
            DominantSide = Truncate(request.DominantSide, 10),
            FitnessLevel = Truncate(request.FitnessLevel, 20),
            InjuryStatus = Truncate(request.InjuryStatus, 20),
            CoachEmail = Truncate(request.CoachEmail, 100),
            CoachPhone = Truncate(request.CoachPhone, 20),
        };

        var created = await _playerService.CreatePlayerAsync(player);
        return CreatedAtAction(nameof(GetPlayerDetails), new { playerId = created.PlayerId }, created);
    }

    [HttpPut("{playerId}")]
    public async Task<ActionResult<PlayerEntity>> UpdatePlayer(string playerId, [FromBody] CreatePlayerRequest request)
    {
        if (!Guid.TryParse(playerId, out _))
        {
            return BadRequest(new { message = "playerId must be a valid GUID." });
        }

        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(access.RestrictToScoutId))
        {
            var existing = await _playerService.GetPlayerDetailsAsync(playerId);
            if (existing == null)
            {
                return NotFound(new { message = $"Player with ID '{playerId}' not found." });
            }

            if (!string.Equals(existing.ScoutId, access.RestrictToScoutId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            request.AgentScoutId = access.RestrictToScoutId;
        }

        // If logged-in user is a Player, ensure they can only update their own profile
        if (!string.IsNullOrWhiteSpace(access.LoggedInPlayerId))
        {
            if (!string.Equals(playerId, access.LoggedInPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        var player = new PlayerEntity
        {
            PlayerId = playerId,
            FullName = Truncate(request.FullName, 150),
            DateOfBirth = string.IsNullOrEmpty(request.DateOfBirth) ? null : DateOnly.Parse(request.DateOfBirth),
            Nationality = Truncate(request.Nationality, 100),
            PositionCode = Truncate(request.Position, 10),
            PreferredFoot = Truncate(request.PreferredFoot, 10),
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            CurrentClubId = Truncate(request.CurrentClub, 50),
            ContractStartDate = string.IsNullOrEmpty(request.ContractStart) ? null : DateOnly.Parse(request.ContractStart),
            ContractEndDate = string.IsNullOrEmpty(request.ContractEnd) ? null : DateOnly.Parse(request.ContractEnd),
            ContractStartWithCoach = string.IsNullOrEmpty(request.ContractStartWithCoach) ? null : DateOnly.Parse(request.ContractStartWithCoach),
            ContractEndWithCoach = string.IsNullOrEmpty(request.ContractEndWithCoach) ? null : DateOnly.Parse(request.ContractEndWithCoach),
            AgentName = Truncate(request.AgentName, 150),
            AgentScoutId = Truncate(request.AgentScoutId, 50),
            ContactInfo = Truncate(request.ContactInfo, 255),
            PlayerEmail = Truncate(request.PlayerEmail, 255),
            SportId = request.SportId,
            // extended fields
            Gender = Truncate(request.Gender, 10),
            PlaceOfBirth = Truncate(request.PlaceOfBirth, 100),
            PrimaryLanguage = Truncate(request.PrimaryLanguage, 50),
            SecondaryLanguage = Truncate(request.SecondaryLanguage, 50),
            ProfileVisibility = request.ProfileVisibility,
            PhoneNumber = Truncate(request.PhoneNumber, 20),
            AlternatePhone = Truncate(request.AlternatePhone, 20),
            EmergencyContactName = Truncate(request.EmergencyContactName, 100),
            EmergencyContactNumber = Truncate(request.EmergencyContactNumber, 20),
            AddressLine1 = Truncate(request.AddressLine1, 150),
            AddressLine2 = Truncate(request.AddressLine2, 150),
            City = Truncate(request.City, 100),
            State = Truncate(request.State, 100),
            Country = Truncate(request.Country, 100),
            PostalCode = Truncate(request.PostalCode, 20),
            SecondaryPosition = Truncate(request.SecondaryPosition, 50),
            JerseyNumber = request.JerseyNumber,
            ExperienceYears = request.ExperienceYears,
            PlayingLevel = Truncate(request.PlayingLevel, 20),
            DominantSide = Truncate(request.DominantSide, 10),
            FitnessLevel = Truncate(request.FitnessLevel, 20),
            InjuryStatus = Truncate(request.InjuryStatus, 20),
            CoachEmail = Truncate(request.CoachEmail, 100),
            CoachPhone = Truncate(request.CoachPhone, 20),
        };

        var updated = await _playerService.UpdatePlayerAsync(player);
        if (updated == null) return NotFound();

        // Return refreshed PlayerDetailsResponse so client gets canonical shape and updated fields
        var refreshed = await _playerService.GetPlayerDetailsAsync(playerId);
        return Ok(refreshed);
    }

    [HttpDelete("{playerId}")]
    public async Task<IActionResult> DeletePlayer(string playerId)
    {
        if (!Guid.TryParse(playerId, out _))
        {
            return BadRequest(new { message = "playerId must be a valid GUID." });
        }

        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(access.RestrictToScoutId))
        {
            var existing = await _playerService.GetPlayerDetailsAsync(playerId);
            if (existing == null)
            {
                return NotFound(new { message = $"Player with ID '{playerId}' not found." });
            }

            if (!string.Equals(existing.ScoutId, access.RestrictToScoutId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        var deleted = await _playerService.DeletePlayerAsync(playerId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("playerprofileimg")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPlayerProfileImage([FromForm] Models.Requests.PlayerProfileImageUploadRequest request)
    {
        var playerId = request.PlayerId;
        var file = request.File;

        if (string.IsNullOrWhiteSpace(playerId))
            return BadRequest(new { message = "playerId is required" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var access = await ResolvePlayerAccessScopeAsync();
        if (access == null) return Forbid();

        // ensure player exists
        var player = await _footballContext.Players1.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null) return NotFound(new { message = $"Player '{playerId}' not found" });

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "player-profiles");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Remove any existing files for this player
        var existing = Directory.GetFiles(folderPath, $"player-{playerId}.*");
        foreach (var f in existing)
        {
            try { System.IO.File.Delete(f); } catch { }
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"player-{playerId}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"{Request.Scheme}://{Request.Host}/player-profiles/{fileName}";

        // Update DB (players table mapping Player1)
        player.ProfileImageUrl = imageUrl;
        player.UpdatedAt = DateTime.UtcNow;
        await _footballContext.SaveChangesAsync();

        var refreshed = await _playerService.GetPlayerDetailsAsync(playerId);
        return Ok(new { imageUrl, player = refreshed });
    }

    private async Task<PlayerAccessScope?> ResolvePlayerAccessScopeAsync()
    {
        var roleValues = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        var userEmail = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? User.Identity?.Name;

        var isAdminSeedAccount = !string.IsNullOrWhiteSpace(userEmail)
            && userEmail.Equals("admin@footballdashboard.local", StringComparison.OrdinalIgnoreCase);

        var isAdmin = isAdminSeedAccount
            || roleValues.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
            || User.IsInRole("Admin");

        var isScoutOrCoach = roleValues.Any(r =>
                string.Equals(r, "Scout", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r, "Coach", StringComparison.OrdinalIgnoreCase))
            || User.IsInRole("Scout")
            || User.IsInRole("Coach");

        if (!isAdmin && !isScoutOrCoach)
        {
            // Allow Player role users to view the players list (read-only, no scout restriction)
            var isPlayer = roleValues.Any(r => string.Equals(r, "Player", StringComparison.OrdinalIgnoreCase))
                || User.IsInRole("Player");

            if (isPlayer)
            {
                string? loggedInPlayerId = null;
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    var pl = await _footballContext.Players1
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.playerEmail != null && p.playerEmail.ToLower() == userEmail.ToLower());
                    if (pl != null)
                        loggedInPlayerId = pl.PlayerId;
                }

                return new PlayerAccessScope
                {
                    IsAdmin = false,
                    IsScoutOrCoach = false,
                    LoggedInScoutId = null,
                    RestrictToScoutId = null,
                    LoggedInPlayerId = loggedInPlayerId,
                };
            }

            return null;
        }

        if (isAdmin)
        {
            return new PlayerAccessScope
            {
                IsAdmin = true,
                IsScoutOrCoach = false,
                LoggedInScoutId = null,
                RestrictToScoutId = null,
            };
        }

        userEmail = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return new PlayerAccessScope
            {
                IsAdmin = false,
                IsScoutOrCoach = true,
                LoggedInScoutId = null,
                RestrictToScoutId = null,
            };
        }

        var scout = await _footballContext.Scouts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email != null && s.Email.ToLower() == userEmail.ToLower());

        if (scout == null)
        {
            return new PlayerAccessScope
            {
                IsAdmin = false,
                IsScoutOrCoach = true,
                LoggedInScoutId = null,
                RestrictToScoutId = null,
            };
        }

        return new PlayerAccessScope
        {
            IsAdmin = false,
            IsScoutOrCoach = true,
            LoggedInScoutId = scout.ScoutId,
            RestrictToScoutId = scout.IsShowPlayer ? null : scout.ScoutId,
        };
    }

    private async Task<PlayersOtherDataResponse> BuildOtherDataAsync(PlayerAccessScope access)
    {
        var scoutOptions = await _footballContext.Scouts
            .AsNoTracking()
            .OrderBy(s => s.ScoutName)
            .Select(s => new ScoutOptionResponse
            {
                ScoutId = s.ScoutId,
                ScoutName = s.ScoutName
            })
            .ToListAsync();

        var positionOptions = (await _playerPositionService.GetAllAsync())
            .OrderBy(p => p.PositionName)
            .Select(p => new PositionOptionResponse
            {
                PositionId = p.PositionId,
                PositionName = p.PositionName,
                PositionCode = p.PositionCode
            })
            .ToList();

        var sportsOptions = await _footballContext.Sports
            .AsNoTracking()
            .OrderBy(s => s.SportName)
            .Select(s => new SportOptionResponse
            {
                SportId = s.SportId,
                SportName = s.SportName
            })
            .ToListAsync();

        return new PlayersOtherDataResponse
        {
            ScoutOptions = scoutOptions,
            PositionOptions = positionOptions,
            SportsOptions = sportsOptions,
            LoggedInScoutIsShowPlayer = access.IsScoutOrCoach && string.IsNullOrWhiteSpace(access.RestrictToScoutId)
        };
    }
}
