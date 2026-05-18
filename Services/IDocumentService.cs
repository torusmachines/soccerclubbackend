using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IDocumentService
{
    Task<IEnumerable<Document>> GetAllAsync();
    Task<IEnumerable<Document>> GetByPlayerIdAsync(string playerId);
    Task<Document?> GetByIdAsync(string id);
    Task<Document> CreateAsync(CreateDocument dto);
    Task<bool> UpdateAsync(string id, UpdateDocument dto);
    Task<bool> DeleteAsync(string id);
}