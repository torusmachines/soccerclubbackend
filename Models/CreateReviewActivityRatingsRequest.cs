namespace FootballDashboardAPI.Models;

public class ReviewActivityRatingPayload
{
    public int ActivityId { get; set; }
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime? RatingFollowupDate { get; set; }
}

public class CreateReviewActivityRatingsRequest
{
    public string ReviewId { get; set; } = null!;
    public IEnumerable<ReviewActivityRatingPayload> Ratings { get; set; } = new List<ReviewActivityRatingPayload>();
}
