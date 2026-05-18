using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models.Requests;

public class CreateEmailRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("sentByScoutId")]
    public string SentByScoutId { get; set; } = string.Empty;

    [JsonPropertyName("playerId")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("clubId")]
    public string? ClubId { get; set; }
}
