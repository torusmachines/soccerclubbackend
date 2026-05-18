

using FootballDashboardAPI.Models;
using FootballDashboardAPI.Models.Entities;
using FootballDashboardAPI.Models.Responses;

namespace FootballDashboardAPI.Repositories.Interfaces;

public interface IPlayerRepository
{
    Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardAsync();
    Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardFilteredAsync(
        string? positionCode,
        string? scoutId,
        int? sportId,
        string? search,
        string? restrictToScoutId);
    Task<PlayerDetailsResponse?> GetPlayerDetailsAsync(string playerId);
    Task<IEnumerable<PlayerReviewResponse>> GetPlayerReviewsAsync(string playerId);


    Task<Models.Entities.Player?> GetByIdAsync(string id);
    Task<Models.Entities.Player> CreateAsync(Models.Entities.Player player);
    Task<Models.Entities.Player?> UpdateAsync(Models.Entities.Player player);
    Task<bool> DeleteAsync(string id);
}


