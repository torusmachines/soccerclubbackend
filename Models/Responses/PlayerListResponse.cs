namespace FootballDashboardAPI.Models.Responses;

public class PlayerListResponse
{
    public string PlayerId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? PlayerEmail { get; set; }
    public string UserStatus { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public decimal OverallRating { get; set; }
    public string AgencyContractStatus { get; set; } = string.Empty;
    public string? ScoutId { get; set; }
    public string? ScoutName { get; set; }
    public int? SportId { get; set; }
    public string? SportName { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("playerProfileImage")]
    public string? PlayerProfileImage { get; set; }
}