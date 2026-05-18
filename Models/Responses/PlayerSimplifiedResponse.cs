namespace FootballDashboardAPI.Models.Responses;

public class PlayerSimplifiedResponse
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int? SportId { get; set; }
    public string? SportName { get; set; }
}
