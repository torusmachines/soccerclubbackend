using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace FootballDashboardAPI.Services;

public class TaskCommentService : ITaskCommentService
{
    private readonly PostgresConnectionProvider _db;

    public TaskCommentService(PostgresConnectionProvider db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TaskComment>> GetCommentsByTaskAsync(string taskId, int page = 1, int pageSize = 20)
    {
        const string sql = @"
SELECT tc.comment_id,
       tc.task_id,
       tc.user_id,
       u.""FullName"" AS user_name,
       tc.comment,
       tc.created_at,
       tc.updated_at,
       tc.is_deleted,
       tc.is_visible_to_player
FROM stf.task_comments tc
LEFT JOIN auth.""AspNetUsers"" u ON u.""Id"" = tc.user_id
WHERE tc.task_id = @p_task_id
  AND tc.is_deleted = false
ORDER BY tc.created_at DESC
LIMIT @p_page_size OFFSET (@p_page - 1) * @p_page_size";

        return await _db.ExecuteQueryListAsync(
            sql,
            MapReaderToTaskComment,
            new NpgsqlParameter("p_task_id", NpgsqlDbType.Varchar) { Value = taskId },
            new NpgsqlParameter("p_page", NpgsqlDbType.Integer) { Value = page },
            new NpgsqlParameter("p_page_size", NpgsqlDbType.Integer) { Value = pageSize }
        );
    }

    public async Task<TaskComment?> CreateCommentAsync(string taskId, string userId, string comment, bool isVisibleToPlayer = true)
    {
        var commentId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        const string sql = @"
WITH inserted AS (
  INSERT INTO stf.task_comments (comment_id, task_id, user_id, comment, created_at, is_deleted, is_visible_to_player)
  VALUES (@p_comment_id, @p_task_id, @p_user_id, @p_comment, @p_created_at, false, @p_is_visible_to_player)
  RETURNING comment_id, task_id, user_id, comment, created_at, updated_at, is_deleted, is_visible_to_player
)
SELECT i.comment_id,
       i.task_id,
       i.user_id,
       u.""FullName"" AS user_name,
       i.comment,
       i.created_at,
       i.updated_at,
       i.is_deleted,
       i.is_visible_to_player
FROM inserted i
LEFT JOIN auth.""AspNetUsers"" u ON u.""Id"" = i.user_id";

        return await _db.ExecuteQuerySingleAsync(
            sql,
            MapReaderToTaskComment,
            new NpgsqlParameter("p_comment_id", NpgsqlDbType.Uuid) { Value = commentId },
            new NpgsqlParameter("p_task_id", NpgsqlDbType.Varchar) { Value = taskId },
            new NpgsqlParameter("p_user_id", NpgsqlDbType.Varchar) { Value = userId },
            new NpgsqlParameter("p_comment", NpgsqlDbType.Text) { Value = comment },
            new NpgsqlParameter("p_created_at", NpgsqlDbType.Timestamp) { Value = DateTime.SpecifyKind(createdAt, DateTimeKind.Unspecified) },
            new NpgsqlParameter("p_is_visible_to_player", NpgsqlDbType.Boolean) { Value = isVisibleToPlayer }
        );
    }

    public async Task<TaskComment?> UpdateCommentAsync(Guid commentId, string comment, bool isVisibleToPlayer)
    {
        const string sql = @"
WITH updated AS (
  UPDATE stf.task_comments
  SET comment = @p_comment,
      is_visible_to_player = @p_is_visible_to_player,
      updated_at = @p_updated_at
  WHERE comment_id = @p_comment_id
    AND is_deleted = false
  RETURNING comment_id, task_id, user_id, comment, created_at, updated_at, is_deleted, is_visible_to_player
)
SELECT u.""FullName"" AS user_name,
       updated.comment_id,
       updated.task_id,
       updated.user_id,
       updated.comment,
       updated.created_at,
       updated.updated_at,
       updated.is_deleted,
       updated.is_visible_to_player
FROM updated
LEFT JOIN auth.""AspNetUsers"" u ON u.""Id"" = updated.user_id";

        return await _db.ExecuteQuerySingleAsync(
            sql,
            MapReaderToTaskComment,
            new NpgsqlParameter("p_comment_id", NpgsqlDbType.Uuid) { Value = commentId },
            new NpgsqlParameter("p_comment", NpgsqlDbType.Text) { Value = comment },
            new NpgsqlParameter("p_is_visible_to_player", NpgsqlDbType.Boolean) { Value = isVisibleToPlayer },
            new NpgsqlParameter("p_updated_at", NpgsqlDbType.Timestamp) { Value = DateTime.UtcNow }
        );
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId)
    {
        const string sql = @"
UPDATE stf.task_comments
SET is_deleted = true,
    updated_at = @p_updated_at
WHERE comment_id = @p_comment_id";

        var rows = await _db.ExecuteNonQueryAsync(
            sql,
            new NpgsqlParameter("p_comment_id", NpgsqlDbType.Uuid) { Value = commentId },
            new NpgsqlParameter("p_updated_at", NpgsqlDbType.Timestamp) { Value = DateTime.UtcNow }
        );

        return rows > 0;
    }

    private TaskComment MapReaderToTaskComment(NpgsqlDataReader reader)
    {
        return new TaskComment
        {
            CommentId = reader.GetGuid(reader.GetOrdinal("comment_id")),
            TaskId = reader["task_id"].ToString()!,
            UserId = reader["user_id"].ToString()!,
            UserName = reader["user_name"] == DBNull.Value ? "Unknown" : reader["user_name"].ToString()!,
            Comment = reader["comment"].ToString()!,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
            IsDeleted = (bool)reader["is_deleted"],
            IsVisibleToPlayer = (bool)reader["is_visible_to_player"]
        };
    }
}
