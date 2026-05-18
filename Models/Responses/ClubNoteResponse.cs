namespace FootballDashboardAPI.Models.Responses;

public class ClubNoteResponse
{
    public string NoteId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateOnly? FollowUpDate { get; set; }
    public string CreatedByScoutId { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public bool IsVisibleToPlayer { get; set; }
}
