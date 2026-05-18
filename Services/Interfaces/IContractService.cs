using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services.Interfaces;

public interface IContractService
{
    Task<IEnumerable<ContractResponse>> GetContractsAsync(ContractQueryParameters filters);
    Task<IEnumerable<ContractResponse>> GetContractAlertsAsync(string? contractType, int daysAhead, int? limit);
    Task<ContractResponse?> GetContractByIdAsync(Guid id);
    Task<ContractResponse> CreateContractAsync(Contract contract);
    Task<ContractResponse?> UpdateContractAsync(Guid id, Contract contract);
    Task<bool> DeleteContractAsync(Guid id);
}
