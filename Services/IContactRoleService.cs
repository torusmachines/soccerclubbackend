using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IContactRoleService
{
    Task<IEnumerable<ContactRole>> GetAllAsync();
    Task<ContactRole?> GetByIdAsync(string id);
    Task<ContactRole> CreateAsync(CreateContactRole dto, string createdBy);
    Task<ContactRole?> UpdateAsync(string id, UpdateContactRole dto);
    Task<bool> DeleteAsync(string id);
}