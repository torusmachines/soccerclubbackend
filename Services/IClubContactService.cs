using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IClubContactService
{
    Task<IEnumerable<ClubContact>> GetAllAsync();
    Task<ClubContact?> GetByIdAsync(string id);
    Task<IEnumerable<ClubContact>> GetByClubIdAsync(string clubId);
    Task<ClubContact> CreateAsync(CreateClubContact dto);
    Task<ClubContact?> UpdateAsync(string id, UpdateClubContact dto);
    Task<bool> DeleteAsync(string id);
}