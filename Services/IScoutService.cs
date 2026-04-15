using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IScoutService
{
    Task<IEnumerable<Scout>> GetAllScoutsAsync();
    Task<Scout?> GetScoutByIdAsync(string id);
    Task<IEnumerable<Scout>> GetScoutsBySportIdAsync(int sportId);
    Task<Scout> CreateScoutAsync(CreateScout createScoutDto);
    Task<Scout?> UpdateScoutAsync(string id, UpdateScout updateScoutDto);
    Task<bool> DeleteScoutAsync(string id);
}
