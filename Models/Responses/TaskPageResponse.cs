namespace FootballDashboardAPI.Models.Responses;

public class TaskPageResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public string AssignedById { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedToId { get; set; } = string.Empty;
    public string AssignedToType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusFromTable { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
}
