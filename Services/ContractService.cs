using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories.Interfaces;
using FootballDashboardAPI.Services.Interfaces;

namespace FootballDashboardAPI.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;

    public ContractService(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public Task<IEnumerable<ContractResponse>> GetContractsAsync(ContractQueryParameters filters)
    {
        return _contractRepository.GetContractsAsync(filters);
    }

    public Task<IEnumerable<ContractResponse>> GetContractAlertsAsync(string? contractType, int daysAhead, int? limit)
    {
        return _contractRepository.GetContractAlertsAsync(contractType, daysAhead, limit);
    }

    public Task<ContractResponse?> GetContractByIdAsync(Guid id)
    {
        return _contractRepository.GetByIdAsync(id);
    }

    public async Task<ContractResponse> CreateContractAsync(Contract contract)
    {
        var created = await _contractRepository.CreateAsync(contract);
        return new ContractResponse
        {
            Id = created.Id,
            Party1Id = created.Party1Id,
            Party1Type = created.Party1Type,
            Party1Name = created.Party1Name,
            Party2Id = created.Party2Id,
            Party2Type = created.Party2Type,
            Party2Name = created.Party2Name,
            ContractType = created.ContractType,
            StartDate = created.StartDate,
            EndDate = created.EndDate,
            ExpiryDate = created.ExpiryDate,
            ContractDetails = created.ContractDetails,
            DocumentPath = created.DocumentPath,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            ExpiryStatus = GetExpiryStatus(created)
        };
    }

    public async Task<ContractResponse?> UpdateContractAsync(Guid id, Contract contract)
    {
        if (contract.Id != id)
            contract.Id = id;

        var updated = await _contractRepository.UpdateAsync(contract);
        if (updated == null)
            return null;

        return new ContractResponse
        {
            Id = updated.Id,
            Party1Id = updated.Party1Id,
            Party1Type = updated.Party1Type,
            Party1Name = updated.Party1Name,
            Party2Id = updated.Party2Id,
            Party2Type = updated.Party2Type,
            Party2Name = updated.Party2Name,
            ContractType = updated.ContractType,
            StartDate = updated.StartDate,
            EndDate = updated.EndDate,
            ExpiryDate = updated.ExpiryDate,
            ContractDetails = updated.ContractDetails,
            DocumentPath = updated.DocumentPath,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            ExpiryStatus = GetExpiryStatus(updated)
        };
    }

    public Task<bool> DeleteContractAsync(Guid id)
    {
        return _contractRepository.DeleteAsync(id);
    }

    private static string GetExpiryStatus(Contract contract)
    {
        var today = DateTime.UtcNow.Date;
        var compareDate = contract.ExpiryDate ?? contract.EndDate;

        if (compareDate < today)
            return "Expired";

        if (compareDate <= today.AddDays(30))
            return "ExpiringSoon";

        return "Active";
    }
}
