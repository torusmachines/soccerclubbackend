using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ReviewRatingRepository : IReviewRatingRepository
{
    private readonly FootballContext _footballContext;

    public ReviewRatingRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<ReviewRating>> GetAllAsync()
    {
        return await _footballContext.ReviewRatings
            .AsNoTracking()
            .OrderBy(r => r.ReviewId)
            .ToListAsync();
    }

    public async Task<ReviewRating?> GetByIdAsync(string id)
    {
        return await _footballContext.ReviewRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReviewId == id);
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _footballContext.ReviewRatings
            .AsNoTracking()
            .AnyAsync(r => r.ReviewId == id);
    }

    public async Task<ReviewRating> CreateAsync(ReviewRating rating)
    {
        _footballContext.ReviewRatings.Add(rating);
        await _footballContext.SaveChangesAsync();

        return rating;
    }

    public async Task<ReviewRating?> UpdateAsync(ReviewRating rating)
    {
        var existing = await _footballContext.ReviewRatings
            .FirstOrDefaultAsync(r => r.ReviewId == rating.ReviewId);

        if (existing == null) return null;

        existing.Passing = rating.Passing;
        existing.Shooting = rating.Shooting;
        existing.Dribbling = rating.Dribbling;
        existing.TacticalAwareness = rating.TacticalAwareness;
        existing.DefensiveContribution = rating.DefensiveContribution;
        existing.PhysicalStrength = rating.PhysicalStrength;
        existing.Behavior = rating.Behavior;
        existing.OverallPerformance = rating.OverallPerformance;

        await _footballContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _footballContext.ReviewRatings
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (existing == null)
            return false;

        _footballContext.ReviewRatings.Remove(existing);
        return await _footballContext.SaveChangesAsync() > 0;
    }
}
