namespace FootballDashboardAPI.Models.Responses;

public class PlayerReviewResponse
{
    public string ReviewId { get; set; } = string.Empty;
    public string ScoutId { get; set; } = string.Empty;
    public string ScoutName { get; set; } = string.Empty;
    public DateOnly? MatchDate { get; set; }
    public string Club1Name { get; set; } = string.Empty;
    public string Club2Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public List<ReviewActivityResponse> Activities { get; set; } = new();
}

public class ReviewActivityResponse
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}