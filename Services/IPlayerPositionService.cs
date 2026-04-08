namespace FootballDashboardAPI.Services;

using FootballDashboardAPI.Models;

public interface IPlayerPositionService
{
    Task<IEnumerable<PlayerPosition>> GetAllAsync();
    Task<PlayerPosition?> GetByIdAsync(string id);
    Task<PlayerPosition> CreateAsync(CreatePlayerPosition dto, string createdBy);
    Task<PlayerPosition?> UpdateAsync(string id, UpdatePlayerPosition dto);
    Task<bool> DeleteAsync(string id);
}
