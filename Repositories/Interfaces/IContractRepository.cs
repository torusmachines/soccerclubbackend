using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Repositories.Interfaces;

public interface IContractRepository
{
    Task<IEnumerable<ContractResponse>> GetContractsAsync(ContractQueryParameters filters);
    Task<IEnumerable<ContractResponse>> GetContractAlertsAsync(string? contractType, int daysAhead, int? limit);
    Task<ContractResponse?> GetByIdAsync(Guid id);
    Task<Contract> CreateAsync(Contract contract);
    Task<Contract?> UpdateAsync(Contract contract);
    Task<bool> DeleteAsync(Guid id);
}
