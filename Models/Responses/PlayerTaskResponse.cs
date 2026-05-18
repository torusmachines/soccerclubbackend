namespace FootballDashboardAPI.Models.Responses;

public class PlayerTaskResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    // player name
    public string TaskAssignedToPlayer { get; set; } = string.Empty;

    // scout who created/assigned
    public string AssignedToScoutId { get; set; } = string.Empty;

    // New properties for task assignment and creation details
    public string? AssignedToID { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
}