using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Repositories;

public class ReviewActivityRatingRepository : IReviewActivityRatingRepository
{
    private readonly FootballContext _footballContext;

    public ReviewActivityRatingRepository(FootballContext footballContext)
    {
        _footballContext = footballContext;
    }

    public async Task<IEnumerable<ReviewActivityRating>> GetAllAsync()
    {
        return await _footballContext.ReviewActivityRatings
            .AsNoTracking()
            .OrderBy(r => r.ReviewActivityRatingId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewActivityRating>> GetByReviewIdAsync(string reviewId)
    {
        return await _footballContext.ReviewActivityRatings
            .AsNoTracking()
            .Where(r => r.ReviewId == reviewId)
            .OrderBy(r => r.ReviewActivityRatingId)
            .ToListAsync();
    }

    public async Task<ReviewActivityRating> CreateAsync(ReviewActivityRating rating)
    {
        if (rating.CreatedAt == default)
            rating.CreatedAt = DateTime.UtcNow;

        rating.UpdatedAt = DateTime.UtcNow;

        _footballContext.ReviewActivityRatings.Add(rating);
        await _footballContext.SaveChangesAsync();

        return rating;
    }
}
