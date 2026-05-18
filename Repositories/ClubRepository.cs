using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ClubRepository : IClubRepository
{
    private readonly FootballContext _footballContext;

    public ClubRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<Club>> GetAllAsync()
    {
        return await _footballContext.Clubs
            .AsNoTracking()
            .OrderBy(c => c.ClubName)
            .ToListAsync();
    }

    public async Task<IEnumerable<FootballDashboardAPI.Models.ClubDto>> GetAllWithContactCountsAsync()
    {
        var query = from c in _footballContext.Clubs.AsNoTracking()
                    join cc in _footballContext.ClubContacts on c.ClubId equals cc.ClubId into g
                    select new FootballDashboardAPI.Models.ClubDto
                    {
                        ClubId = c.ClubId,
                        ClubName = c.ClubName,
                        Country = c.Country,
                        AddressLine = c.AddressLine,
                        LogoUrl = c.LogoUrl,
                        CreatedAt = c.CreatedAt,
                        ClubContactCount = g.Count()
                    };

        return await query.OrderBy(c => c.ClubName).ToListAsync();
    }

    public async Task<Club?> GetByIdAsync(string id)
    {
        return await _footballContext.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClubId == id);
    }

    public async Task<FootballDashboardAPI.Models.ClubDto?> GetByIdWithContactCountAsync(string id)
    {
        var query = _footballContext.Clubs
            .AsNoTracking()
            .Where(c => c.ClubId == id)
            .Select(c => new FootballDashboardAPI.Models.ClubDto
            {
                ClubId = c.ClubId,
                ClubName = c.ClubName,
                Country = c.Country,
                AddressLine = c.AddressLine,
                LogoUrl = c.LogoUrl,
                CreatedAt = c.CreatedAt,
                ClubContactCount = c.ClubContacts.Count
            });

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Club> CreateAsync(Club club)
    {
        // Ensure a GUID is used for the club identifier so it's unique across environments
        if (string.IsNullOrWhiteSpace(club.ClubId))
        {
            club.ClubId = Guid.NewGuid().ToString();
        }

        if (club.CreatedAt == default)
        {
            club.CreatedAt = DateTime.UtcNow;
        }

        _footballContext.Clubs.Add(club);
        await _footballContext.SaveChangesAsync();

        return club;
    }

    public async Task<Club?> UpdateAsync(Club club)
    {
        var existing = await _footballContext.Clubs
            .FirstOrDefaultAsync(c => c.ClubId == club.ClubId);

        if (existing == null)
            return null;

        existing.ClubName = club.ClubName;
        existing.Country = club.Country;
        existing.AddressLine = club.AddressLine;
        existing.LogoUrl = club.LogoUrl;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Clubs
            .FirstOrDefaultAsync(c => c.ClubId == id);

        if (existing == null)
            return false;

        _footballContext.Clubs.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.Clubs
            .AsNoTracking()
            .AnyAsync(c => c.ClubId == id);
    }

    public async Task<bool> ClubNameExistsAsync(string clubName, string? excludeClubId = null)
    {
        var normalizedName = clubName.Trim().ToLower();

        return await _footballContext.Clubs
            .AsNoTracking()
            .AnyAsync(c =>
                c.ClubId != excludeClubId &&
                c.ClubName.ToLower() == normalizedName);
    }

    public async Task<FootballDashboardAPI.Models.Responses.ClubDetailsResponse?> GetClubDetailsWithPlayersAsync(string id)
    {
        var club = await _footballContext.Clubs
            .AsNoTracking()
            .Where(c => c.ClubId == id)
            .Select(c => new FootballDashboardAPI.Models.ClubDto
            {
                ClubId = c.ClubId,
                ClubName = c.ClubName,
                Country = c.Country,
                AddressLine = c.AddressLine,
                LogoUrl = c.LogoUrl,
                CreatedAt = c.CreatedAt,
                ClubContactCount = c.ClubContacts.Count
            })
            .FirstOrDefaultAsync();

        if (club == null) return null;

        var players = await _footballContext.Players1
            .AsNoTracking()
            .Where(p => p.CurrentClubId == id || p.CurrentClubId == club.ClubId)
            .OrderBy(p => p.FullName)
            .Select(p => new FootballDashboardAPI.Models.Responses.PlayerAtClubDto
            {
                PlayerId = p.PlayerId,
                PlayerName = p.FullName,
                Position = p.PositionCode,
                ContractStartDate = p.ContractStartDate,
                ContractEndDate = p.ContractEndDate,
                Nationality = p.Nationality
            })
            .ToListAsync();

        var contacts = await _footballContext.ClubContacts
            .AsNoTracking()
            .Where(cc => cc.ClubId == id)
            .OrderBy(cc => cc.ContactName)
            .Select(cc => new FootballDashboardAPI.Models.Responses.ClubContactDto
            {
                ClubContactId = cc.ClubContactId,
                ClubId = cc.ClubId,
                ContactName = cc.ContactName,
                RoleName = cc.RoleName,
                Email = cc.Email,
                Phone = cc.Phone,
                CreatedAt = cc.CreatedAt
            })
            .ToListAsync();

        // club notes
        var notes = await _footballContext.Notes
            .AsNoTracking()
            .Where(n => n.ClubId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new FootballDashboardAPI.Models.Responses.ClubNoteResponse
            {
                NoteId = n.NoteId,
                Topic = n.Topic ?? string.Empty,
                Description = n.Description ?? string.Empty,
                Category = n.Category ?? string.Empty,
                FollowUpDate = n.FollowUpDate,
                CreatedByScoutId = n.CreatedByScoutId ?? string.Empty,
                CreatedAt = n.CreatedAt,
                IsVisibleToPlayer = n.IsVisibleToPlayer
            })
            .ToListAsync();

        // club documents
        var docs = await _footballContext.Documents
            .AsNoTracking()
            .Where(d => d.ClubId == id)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new FootballDashboardAPI.Models.Responses.ClubDocumentResponse
            {
                DocumentId = d.DocumentId,
                DocumentName = d.DocumentName ?? string.Empty,
                DocumentType = d.DocumentType ?? string.Empty,
                DocumentDate = d.DocumentDate,
                FileSizeLabel = d.FileSizeLabel ?? string.Empty,
                FileExtension = string.Empty,
                CreatedAt = d.CreatedAt,
                IsVisibleToPlayer = d.IsVisibleToPlayer
            })
            .ToListAsync();

        // club emails/communication
        var emails = await _footballContext.Emails
            .AsNoTracking()
            .Where(e => e.ClubId == id)
            .OrderByDescending(e => e.SentAt)
            .Select(e => new FootballDashboardAPI.Models.Responses.ClubEmailResponse
            {
                EmailId = e.EmailId,
                RecipientEmail = e.RecipientEmail ?? string.Empty,
                Subject = e.Subject ?? string.Empty,
                Body = e.Body ?? string.Empty,
                SentByScoutId = e.SentByScoutId ?? string.Empty,
                SentAt = e.SentAt
            })
            .ToListAsync();

        // club tasks
        var clubTasks = await _footballContext.Tasks
            .AsNoTracking()
            .Where(t => t.ClubId == id)
            .OrderBy(t => t.DueDate)
            .Select(t => new FootballDashboardAPI.Models.Responses.PlayerTaskResponse
            {
                TaskId = t.TaskId,
                Title = t.Title ?? string.Empty,
                Description = t.Description ?? string.Empty,
                DueDate = t.DueDate,
                Status = t.Status ?? string.Empty,
                Source = t.Source ?? string.Empty,
                AssignedToScoutId = t.AssignedToScoutId ?? string.Empty,
                TaskAssignedToPlayer = t.Player != null ? t.Player.FullName : string.Empty,
                AssignedToID = t.AssignedToScoutId,
                AssignedToName = string.IsNullOrWhiteSpace(t.AssignedToScoutId)
                    ? "Auto-generated"
                    : _footballContext.Scouts
                        .Where(s => s.ScoutId == t.AssignedToScoutId)
                        .Select(s => s.ScoutName)
                        .FirstOrDefault(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        // templates (for compose email templates)
        var clubTemplates = await _footballContext.Templates
            .AsNoTracking()
            .OrderBy(t => t.TemplateName)
            .Select(t => new FootballDashboardAPI.Models.Responses.PlayerDetailsTemplateResponse
            {
                templateId = t.TemplateId,
                templateName = t.TemplateName,
                templateType = t.TemplateType,
                subject = t.Subject ?? string.Empty,
                body = t.Body ?? string.Empty,
            })
            .ToListAsync();

        return new FootballDashboardAPI.Models.Responses.ClubDetailsResponse
        {
            ClubDetails = club,
            PlayersAtClub = players,
            AllContactsForClubs = contacts,
            club_all_notes = notes,
            club_all_documents = docs,
            club_all_emails = emails
            ,
            club_all_tasks = clubTasks,
            clubDetailsTemplates = clubTemplates
        };
    }
}