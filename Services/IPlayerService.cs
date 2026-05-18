
using FootballDashboardAPI.Models.Responses;
using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services.Interfaces;

public interface IPlayerService
{
    Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardAsync();
    Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardFilteredAsync(
        string? positionCode,
        string? scoutId,
        int? sportId,
        string? search,
        string? restrictToScoutId);
    Task<PlayerDetailsResponse?> GetPlayerDetailsAsync(string playerId);
    Task<Models.Entities.Player> CreatePlayerAsync(Models.Entities.Player player);
    Task<Models.Entities.Player?> UpdatePlayerAsync(Models.Entities.Player player);
    Task<bool> DeletePlayerAsync(string playerId);
}