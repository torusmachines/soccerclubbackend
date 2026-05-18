namespace FootballDashboardAPI.Models.Responses;

public class PlayerEmailResponse
{
    public string EmailId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SentByScoutId { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
}