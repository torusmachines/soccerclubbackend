using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ScoutRepository : IScoutRepository
{
    private readonly FootballContext _context;

    public ScoutRepository(FootballContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Scout>> GetAllAsync()
    {
        return await _context.Scouts
            .AsNoTracking()
            .Where(s => s.IsDeleted != true)
            .OrderBy(s => s.ScoutId)
            .ToListAsync();
    }

    public async Task<Scout?> GetByIdAsync(string id)
    {
        return await _context.Scouts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScoutId == id && s.IsDeleted != true);
    }

    public async Task<IEnumerable<Scout>> GetBySportIdAsync(int sportId)
    {
        return await _context.Scouts
            .AsNoTracking()
            .Where(s => s.SportId == sportId && s.IsDeleted != true)
            .OrderBy(s => s.ScoutId)
            .ToListAsync();
    }

    public async Task<string?> GetMaxScoutIdAsync()
    {
        var numericIds = await _context.Scouts
            .AsNoTracking()
            .Where(s => s.IsDeleted != true && !string.IsNullOrWhiteSpace(s.ScoutId))
            .Select(s => s.ScoutId)
            .ToListAsync();

        var maxValue = numericIds
            .Where(id => id.All(char.IsDigit))
            .Select(id => int.Parse(id))
            .DefaultIfEmpty()
            .Max();

        return maxValue == 0 ? null : maxValue.ToString();
    }

    public async Task<Scout> CreateAsync(Scout scout)
    {
        if (scout.CreatedAt == default)
            scout.CreatedAt = DateTime.UtcNow;

        scout.IsDeleted = false;

        _context.Scouts.Add(scout);
        await _context.SaveChangesAsync();

        return scout;
    }

    public async Task<Scout?> UpdateAsync(Scout scout)
    {
        var existing = await _context.Scouts
            .FirstOrDefaultAsync(s => s.ScoutId == scout.ScoutId && s.IsDeleted != true);

        if (existing == null)
            return null;

        existing.ScoutName = scout.ScoutName;
        existing.RoleName = scout.RoleName;
        existing.FirstName = scout.FirstName;
        existing.LastName = scout.LastName;
        existing.Email = scout.Email;
        existing.PhoneNumber = scout.PhoneNumber;
        existing.AddressLine1 = scout.AddressLine1;
        existing.AddressLine2 = scout.AddressLine2;
        existing.City = scout.City;
        existing.State = scout.State;
        existing.PostalCode = scout.PostalCode;
        existing.Country = scout.Country;
        existing.LockedAreas = scout.LockedAreas;
        existing.IsShowPlayer = scout.IsShowPlayer;
        existing.SportId = scout.SportId;
        existing.UserStatus = scout.UserStatus;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var scout = await _context.Scouts
            .FirstOrDefaultAsync(s => s.ScoutId == id && s.IsDeleted != true);

        if (scout == null)
            return false;

        scout.IsDeleted = true;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Scouts
            .AsNoTracking()
            .AnyAsync(s => s.ScoutId == id && s.IsDeleted != true);
    }

    public async Task<bool> ScoutNameExistsAsync(string scoutName, string? excludeScoutId = null)
    {
        var normalizedName = scoutName.Trim().ToLower();

        return await _context.Scouts
            .AsNoTracking()
            .Where(s => s.IsDeleted != true)
            .Where(s => s.ScoutName.ToLower() == normalizedName)
            .Where(s => excludeScoutId == null || s.ScoutId != excludeScoutId)
            .AnyAsync();
    }
}
