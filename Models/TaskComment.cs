using System;

namespace FootballDashboardAPI.Models;

public class TaskComment
{
    public Guid CommentId { get; set; }
    public string TaskId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsVisibleToPlayer { get; set; }
}
