using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Repositories;

public interface IReviewActivityRatingRepository
{
    Task<IEnumerable<ReviewActivityRating>> GetAllAsync();
    Task<IEnumerable<ReviewActivityRating>> GetByReviewIdAsync(string reviewId);
    Task<ReviewActivityRating> CreateAsync(ReviewActivityRating rating);
}
