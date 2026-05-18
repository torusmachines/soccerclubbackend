using FootballDashboardAPI.Models.Responses;
using FootballDashboardAPI.Repositories.Interfaces;
using FootballDashboardAPI.Services.Interfaces;

namespace FootballDashboardAPI.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardAsync()
    {
        return await _playerRepository.GetPlayersDashboardAsync();
    }

    public async Task<IEnumerable<PlayerListResponse>> GetPlayersDashboardFilteredAsync(
        string? positionCode,
        string? scoutId,
        int? sportId,
        string? search,
        string? restrictToScoutId)
    {
        return await _playerRepository.GetPlayersDashboardFilteredAsync(
            positionCode,
            scoutId,
            sportId,
            search,
            restrictToScoutId);
    }

    public async Task<PlayerDetailsResponse?> GetPlayerDetailsAsync(string playerId)
    {
        return await _playerRepository.GetPlayerDetailsAsync(playerId);
    }

    public async Task<FootballDashboardAPI.Models.Entities.Player> CreatePlayerAsync(FootballDashboardAPI.Models.Entities.Player player)
    {
        return await _playerRepository.CreateAsync(player);
    }

    public async Task<FootballDashboardAPI.Models.Entities.Player?> UpdatePlayerAsync(FootballDashboardAPI.Models.Entities.Player player)
    {
        return await _playerRepository.UpdateAsync(player);
    }

    public async Task<bool> DeletePlayerAsync(string playerId)
    {
        return await _playerRepository.DeleteAsync(playerId);
    }
}

