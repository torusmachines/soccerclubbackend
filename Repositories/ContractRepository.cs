using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly FootballContext _footballContext;

    public ContractRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<ContractResponse>> GetContractsAsync(ContractQueryParameters filters)
    {
        var query = _footballContext.Contracts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.ContractType))
        {
            query = query.Where(c => c.ContractType == filters.ContractType);
        }

        if (filters.PartyId.HasValue)
        {
            query = query.Where(c => c.Party1Id == filters.PartyId.Value || c.Party2Id == filters.PartyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.PartyType))
        {
            query = query.Where(c => c.Party1Type == filters.PartyType || c.Party2Type == filters.PartyType);
        }

        if (filters.StartDateFrom.HasValue)
        {
            query = query.Where(c => c.StartDate >= filters.StartDateFrom.Value);
        }

        if (filters.StartDateTo.HasValue)
        {
            query = query.Where(c => c.StartDate <= filters.StartDateTo.Value);
        }

        if (filters.EndDateFrom.HasValue)
        {
            query = query.Where(c => c.EndDate >= filters.EndDateFrom.Value);
        }

        if (filters.EndDateTo.HasValue)
        {
            query = query.Where(c => c.EndDate <= filters.EndDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var searchTerm = filters.Search.Trim();
            var searchLike = $"%{searchTerm}%";

            query = query.Where(c =>
                EF.Functions.ILike(c.ContractDetails ?? string.Empty, searchLike) ||
                EF.Functions.ILike(c.Party1Name ?? string.Empty, searchLike) ||
                EF.Functions.ILike(c.Party2Name ?? string.Empty, searchLike));
        }

        if (!string.IsNullOrWhiteSpace(filters.ExpiryStatus))
        {
            var status = filters.ExpiryStatus.Trim();
            var today = DateTime.UtcNow.Date;

            if (status == "Active")
            {
                query = query.Where(c => (c.ExpiryDate == null && c.EndDate >= today) || (c.ExpiryDate != null && c.ExpiryDate >= today));
            }
            else if (status == "ExpiringSoon")
            {
                query = query.Where(c => (c.ExpiryDate != null && c.ExpiryDate >= today && c.ExpiryDate <= today.AddDays(30)) || (c.ExpiryDate == null && c.EndDate >= today && c.EndDate <= today.AddDays(30)));
            }
            else if (status == "Expired")
            {
                query = query.Where(c => (c.ExpiryDate != null && c.ExpiryDate < today) || (c.ExpiryDate == null && c.EndDate < today));
            }
        }

        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return contracts.Select(c => new ContractResponse
        {
            Id = c.Id,
            Party1Id = c.Party1Id,
            Party1Type = c.Party1Type,
            Party1Name = c.Party1Name,
            Party2Id = c.Party2Id,
            Party2Type = c.Party2Type,
            Party2Name = c.Party2Name,
            ContractType = c.ContractType,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ExpiryDate = c.ExpiryDate,
            ContractDetails = c.ContractDetails,
            DocumentPath = c.DocumentPath,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            ExpiryStatus = GetExpiryStatus(c)
        }).ToList();
    }

    public async Task<IEnumerable<ContractResponse>> GetContractAlertsAsync(string? contractType, int daysAhead, int? limit)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(daysAhead);

        var query = _footballContext.Contracts
            .AsNoTracking()
            .Where(c => c.EndDate >= today && c.EndDate <= cutoff);

        if (!string.IsNullOrWhiteSpace(contractType))
        {
            query = query.Where(c => c.ContractType == contractType);
        }

        query = query.OrderBy(c => c.EndDate);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var contracts = await query.ToListAsync();

        return contracts.Select(c => new ContractResponse
        {
            Id = c.Id,
            Party1Id = c.Party1Id,
            Party1Type = c.Party1Type,
            Party1Name = c.Party1Name,
            Party2Id = c.Party2Id,
            Party2Type = c.Party2Type,
            Party2Name = c.Party2Name,
            ContractType = c.ContractType,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ExpiryDate = c.ExpiryDate,
            ContractDetails = c.ContractDetails,
            DocumentPath = c.DocumentPath,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            ExpiryStatus = GetExpiryStatus(c)
        }).ToList();
    }

    public async Task<ContractResponse?> GetByIdAsync(Guid id)
    {
        var contract = await _footballContext.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            return null;

        return new ContractResponse
        {
            Id = contract.Id,
            Party1Id = contract.Party1Id,
            Party1Type = contract.Party1Type,
            Party1Name = contract.Party1Name,
            Party2Id = contract.Party2Id,
            Party2Type = contract.Party2Type,
            Party2Name = contract.Party2Name,
            ContractType = contract.ContractType,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            ExpiryDate = contract.ExpiryDate,
            ContractDetails = contract.ContractDetails,
            DocumentPath = contract.DocumentPath,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            ExpiryStatus = GetExpiryStatus(contract)
        };
    }

    public async Task<Contract> CreateAsync(Contract contract)
    {
        _footballContext.Contracts.Add(contract);
        await _footballContext.SaveChangesAsync();
        return contract;
    }

    public async Task<Contract?> UpdateAsync(Contract contract)
    {
        var existing = await _footballContext.Contracts.FirstOrDefaultAsync(c => c.Id == contract.Id);
        if (existing == null)
            return null;

        existing.Party1Id = contract.Party1Id;
        existing.Party1Type = contract.Party1Type;
        existing.Party1Name = contract.Party1Name;
        existing.Party2Id = contract.Party2Id;
        existing.Party2Type = contract.Party2Type;
        existing.Party2Name = contract.Party2Name;
        existing.ContractType = contract.ContractType;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.ExpiryDate = contract.ExpiryDate;
        existing.ContractDetails = contract.ContractDetails;
        existing.DocumentPath = contract.DocumentPath;
        existing.UpdatedAt = DateTime.UtcNow;

        await _footballContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contract = await _footballContext.Contracts.FirstOrDefaultAsync(c => c.Id == id);
        if (contract == null)
            return false;

        _footballContext.Contracts.Remove(contract);
        await _footballContext.SaveChangesAsync();
        return true;
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
