using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface ITaskCommentService
{
    Task<IEnumerable<TaskComment>> GetCommentsByTaskAsync(string taskId, int page = 1, int pageSize = 20);
    Task<TaskComment?> CreateCommentAsync(string taskId, string userId, string comment, bool isVisibleToPlayer = true);
    Task<TaskComment?> UpdateCommentAsync(Guid commentId, string comment, bool isVisibleToPlayer);
    Task<bool> DeleteCommentAsync(Guid commentId);
}
