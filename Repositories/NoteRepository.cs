using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly FootballContext _footballContext;

    public NoteRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<Note>> GetAllAsync()
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetByIdAsync(string id)
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NoteId == id);
    }

    public async Task<IEnumerable<Note>> GetByPlayerIdAsync(string playerId)
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> GetByClubIdAsync(string clubId)
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .Where(n => n.ClubId == clubId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.Notes
            .AsNoTracking()
            .AnyAsync(n => n.NoteId == id);
    }

    public async Task<string?> GetMaxNoteIdAsync()
    {
        var noteIds = await _footballContext.Notes
            .AsNoTracking()
            .Select(n => n.NoteId)
            .ToListAsync();

        var maxId = noteIds
            .Select(id => int.TryParse(id, out var parsed) ? (int?)parsed : null)
            .Max();

        return maxId?.ToString();
    }

    public async Task<Note> CreateAsync(Note note)
    {
        note.PlayerId = string.IsNullOrWhiteSpace(note.PlayerId) ? null : note.PlayerId;
        note.ClubId = string.IsNullOrWhiteSpace(note.ClubId) ? null : note.ClubId;

        _footballContext.Notes.Add(note);
        await _footballContext.SaveChangesAsync();

        return note;
    }

    public async Task<Note?> UpdateAsync(Note note)
    {
        var existing = await _footballContext.Notes
            .FirstOrDefaultAsync(n => n.NoteId == note.NoteId);

        if (existing == null)
            return null;

        existing.PlayerId = string.IsNullOrWhiteSpace(note.PlayerId) ? null : note.PlayerId;
        existing.ClubId = string.IsNullOrWhiteSpace(note.ClubId) ? null : note.ClubId;
        existing.Topic = note.Topic;
        existing.Description = note.Description;
        existing.Category = note.Category;
        existing.FollowUpDate = note.FollowUpDate;
        existing.IsVisibleToPlayer = note.IsVisibleToPlayer;
        existing.CreatedByScoutId = note.CreatedByScoutId;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Notes
            .FirstOrDefaultAsync(n => n.NoteId == id);

        if (existing == null)
            return false;

        _footballContext.Notes.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }
}
