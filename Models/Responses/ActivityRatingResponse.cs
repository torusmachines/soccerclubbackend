namespace FootballDashboardAPI.Models.Responses;

public class ActivityRatingResponse
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
}