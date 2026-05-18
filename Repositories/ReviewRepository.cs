using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly FootballContext _footballContext;

    public ReviewRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<Review>> GetAllAsync()
    {
        return await _footballContext.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.MatchDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetByIdAsync(string id)
    {
        return await _footballContext.Reviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReviewId == id);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        if (review.CreatedAt == default)
            review.CreatedAt = DateTime.UtcNow;

        _footballContext.Reviews.Add(review);
        await _footballContext.SaveChangesAsync();

        return review;
    }

    public async Task<Review?> UpdateAsync(Review review)
    {
        var existing = await _footballContext.Reviews
            .FirstOrDefaultAsync(r => r.ReviewId == review.ReviewId);

        if (existing == null)
            return null;

        existing.MatchDate = review.MatchDate;
        existing.Club1Id = review.Club1Id;
        existing.Club2Id = review.Club2Id;
        existing.Notes = review.Notes;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.Reviews
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (existing == null)
            return false;

        _footballContext.Reviews.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.Reviews
            .AsNoTracking()
            .AnyAsync(r => r.ReviewId == id);
    }

    public async Task<IEnumerable<Review>> GetByPlayerIdAsync(string playerId)
    {
        return await _footballContext.Reviews
            .AsNoTracking()
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.MatchDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByScoutIdAsync(string scoutId)
    {
        return await _footballContext.Reviews
            .AsNoTracking()
            .Where(r => r.ScoutId == scoutId)
            .OrderByDescending(r => r.MatchDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    //public async Task<string> GetLastIdAsync()
    //{
    //    var result = await _db.ExecuteScalarAsync(
    //        "SELECT MAX(CAST(SUBSTRING(review_id, 2) AS INTEGER)) FROM stf.reviews WHERE review_id ~ '^r\\d+$'"
    //    );
    //    var maxNumber = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    //    return $"r{maxNumber}";
    //}
    public async Task<long> GetLastIdAsync()
    {
        var reviewIds = await _footballContext.Reviews
            .AsNoTracking()
            .Select(r => r.ReviewId)
            .ToListAsync();

        var lastId = reviewIds
            .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
            .Max();

        return lastId ?? 0;
    }
}
