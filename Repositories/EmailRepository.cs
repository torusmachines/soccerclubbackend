using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly FootballContext _footballContext;

    public EmailRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<Email>> GetAllAsync()
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .OrderByDescending(e => e.SentAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Email>> GetByPlayerIdAsync(string playerId)
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.SentAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Email>> GetByClubIdAsync(string clubId)
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .Where(e => e.ClubId == clubId)
            .OrderByDescending(e => e.SentAt)
            .ToListAsync();
    }

    public async Task<Email?> GetByIdAsync(string id)
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmailId == id);
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.Emails
            .AsNoTracking()
            .AnyAsync(e => e.EmailId == id);
    }

    public async Task<Email> CreateAsync(Email email)
    {
        if (string.IsNullOrWhiteSpace(email.EmailId))
        {
            var existingIds = await _footballContext.Emails
                .AsNoTracking()
                .Select(e => e.EmailId)
                .ToListAsync();

            var lastNumericId = existingIds
                .Select(id => int.TryParse(id, out var parsed) ? (int?)parsed : null)
                .Max();

            email.EmailId = ((lastNumericId ?? 0) + 1).ToString();
        }

        if (email.SentAt == default)
            email.SentAt = DateTime.UtcNow;

        _footballContext.Emails.Add(email);
        await _footballContext.SaveChangesAsync();

        return email;
    }

    public async Task<Email?> UpdateAsync(Email email)
    {
        var existing = await _footballContext.Emails
            .FirstOrDefaultAsync(e => e.EmailId == email.EmailId);

        if (existing == null) return null;

        existing.PlayerId = email.PlayerId;
        existing.ClubId = email.ClubId;
        existing.RecipientEmail = email.RecipientEmail;
        existing.Subject = email.Subject;
        existing.Body = email.Body;
        existing.SentByScoutId = email.SentByScoutId;
        existing.SentAt = email.SentAt;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Emails
            .FirstOrDefaultAsync(e => e.EmailId == id);

        if (existing == null)
            return false;

        _footballContext.Emails.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }
}
