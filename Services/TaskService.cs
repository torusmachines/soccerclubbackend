using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;
using Npgsql;
using NpgsqlTypes;
using Task = FootballDashboardAPI.Models.Task;

namespace FootballDashboardAPI.Services;

public class TaskService : ITaskService
{
    private readonly PostgresConnectionProvider _db;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(PostgresConnectionProvider db, IEmailNotificationService emailService, ILogger<TaskService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<Task>> GetAllTasksAsync()
    {
        return await _db.ExecuteQueryListAsync(
            "SELECT * FROM stf.fn_tasks_get_all()",
            MapReaderToTask
        );
    }

    public async Task<Task?> GetTaskByIdAsync(string id)
    {
        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.fn_tasks_get_by_id(@p_id)",
            MapReaderToTask,
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );
    }

    public async Task<Task> CreateTaskAsync(CreateTask t)
    {
        //var taskId = Guid.NewGuid().ToString();

        var lastIdResult = await _db.ExecuteScalarAsync(
        "SELECT MAX(CAST(task_id AS BIGINT)) FROM stf.tasks"
          );
        var lastId = lastIdResult == null || lastIdResult == DBNull.Value
            ? 0
            : Convert.ToInt64(lastIdResult);
        var taskId = (lastId + 1).ToString();

        var createdAt = DateTime.UtcNow;

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_tasks_insert(@p_task_id, @p_title, @p_assigned_to_scout_id, @p_due_date, @p_status, @p_source, @p_created_at, @p_description, @p_player_id, @p_club_id)",
            new NpgsqlParameter("p_task_id", NpgsqlDbType.Varchar)
            { Value = taskId },
            new NpgsqlParameter("p_title", NpgsqlDbType.Varchar)
            { Value = t.Title },
            new NpgsqlParameter("p_assigned_to_scout_id", NpgsqlDbType.Varchar)
            { Value = t.AssignedToScoutId },
            new NpgsqlParameter("p_due_date", NpgsqlDbType.Date)
            { Value = t.DueDate },
            new NpgsqlParameter("p_status", NpgsqlDbType.Varchar)
            { Value = t.Status },
            new NpgsqlParameter("p_source", NpgsqlDbType.Varchar)
            { Value = t.Source },
            new NpgsqlParameter("p_created_at", NpgsqlDbType.Timestamp)
            { Value = DateTime.SpecifyKind(createdAt, DateTimeKind.Unspecified) },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text)
            { Value = t.Description == null ? DBNull.Value : (object)t.Description },
            new NpgsqlParameter("p_player_id", NpgsqlDbType.Varchar)
            { Value = t.PlayerId == null ? DBNull.Value : (object)t.PlayerId },
            new NpgsqlParameter("p_club_id", NpgsqlDbType.Varchar)
            { Value = t.ClubId == null ? DBNull.Value : (object)t.ClubId }
        );

        // Get the created task record
        var created = await GetTaskByIdAsync(taskId) ?? new Task
        {
            TaskId = taskId,
            Title = t.Title,
            Description = t.Description,
            PlayerId = t.PlayerId,
            ClubId = t.ClubId,
            AssignedToScoutId = t.AssignedToScoutId,
            DueDate = t.DueDate,
            Status = t.Status,
            Source = t.Source,
            CreatedAt = createdAt
        };

        // After creating a task, notify the player (if any).
        // Do NOT send individual per-task emails for tasks created from reviews.
        try
        {
            if (!string.IsNullOrEmpty(created.PlayerId) && !string.Equals(created.Source, "review", StringComparison.OrdinalIgnoreCase))
            {
                var player = await GetPlayerByIdAsync(created.PlayerId!);
                var scout = await GetScoutByIdAsync(created.AssignedToScoutId);
                if (player != null && !string.IsNullOrEmpty(player.playerEmail))
                {
                    // Use SendTaskAssignedAsync to notify the player about a new task.
                    await _emailService.SendTaskAssignedAsync(
                        player.playerEmail!,
                        player.FullName,
                        created.Title,
                        created.DueDate.ToString(),
                        scout?.ScoutName ?? string.Empty
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task created email for task {TaskId}", taskId);
        }

        return created;
    }

    public async Task<Task?> UpdateTaskAsync(string id, UpdateTask dto)
    {
        var existing = await GetTaskByIdAsync(id);
        if (existing == null)
            return null;

        // Determine new status
        var newStatus = dto.Status ?? existing.Status;
        var statusChanged = newStatus != existing.Status;
        var completedNow = statusChanged && newStatus.Equals("closed", StringComparison.OrdinalIgnoreCase);

        await _db.ExecuteNonQueryAsync(
            "SELECT stf.fn_tasks_update(@p_task_id, @p_title, @p_assigned_to_scout_id, @p_due_date, @p_status, @p_source, @p_description, @p_player_id, @p_club_id)",
            new NpgsqlParameter("p_task_id", NpgsqlDbType.Varchar)
            { Value = id },
            new NpgsqlParameter("p_title", NpgsqlDbType.Varchar)
            { Value = dto.Title ?? existing.Title },
            new NpgsqlParameter("p_assigned_to_scout_id", NpgsqlDbType.Varchar)
            { Value = dto.AssignedToScoutId ?? existing.AssignedToScoutId },
            new NpgsqlParameter("p_due_date", NpgsqlDbType.Date)
            { Value = dto.DueDate ?? existing.DueDate },
            new NpgsqlParameter("p_status", NpgsqlDbType.Varchar)
            { Value = newStatus },
            new NpgsqlParameter("p_source", NpgsqlDbType.Varchar)
            { Value = dto.Source ?? existing.Source },
            new NpgsqlParameter("p_description", NpgsqlDbType.Text)
            { Value = (dto.Description ?? existing.Description) == null ? DBNull.Value : (object)(dto.Description ?? existing.Description)! },
            new NpgsqlParameter("p_player_id", NpgsqlDbType.Varchar)
            { Value = (dto.PlayerId ?? existing.PlayerId) == null ? DBNull.Value : (object)(dto.PlayerId ?? existing.PlayerId)! },
            new NpgsqlParameter("p_club_id", NpgsqlDbType.Varchar)
            { Value = (dto.ClubId ?? existing.ClubId) == null ? DBNull.Value : (object)(dto.ClubId ?? existing.ClubId)! }
        );

        var updated = await GetTaskByIdAsync(id);

        // Send completion email if task was just marked as complete
        if (completedNow && updated != null)
        {
            try
            {
                await SendTaskCompletionEmailAsync(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send task completion email for task {TaskId}", id);
                // Don't throw - email failure shouldn't fail the task update
            }
        }

        // Notify player only about the task update. Do NOT send scout notifications
        // here to avoid duplicate notifications; scout notifications are handled
        // by other flows if required.
        try
        {
            if (updated != null && !string.IsNullOrEmpty(updated.PlayerId))
            {
                var player = await GetPlayerByIdAsync(updated.PlayerId);
                var scout = await GetScoutByIdAsync(updated.AssignedToScoutId);
                                if (player != null && !string.IsNullOrEmpty(player.playerEmail))
                                {
                                        // Use explicit subject format for updates as requested: "Updated Task - Task Name"
                                        var subject = $"Updated Task - {updated.Title}";
                                        var html = $@"
                                        <div style='font-family: Arial, sans-serif; max-width:600px; margin:auto; padding:20px;'>
                                            <div style='background:#3498db; padding:12px; border-radius:6px; color:#fff; font-weight:600;'>Updated Task</div>
                                            <div style='border:1px solid #ddd; padding:16px;'>
                                                <p>Hi <strong>{player.FullName}</strong>,</p>
                                                <p>The task <strong>{updated.Title}</strong> has been updated. Please review the latest details in the dashboard.</p>
                                                <p>Due date: <strong>{updated.DueDate}</strong></p>
                                                <br/>
                                                <p style='color:#888; font-size:12px;'>— Football Scout Dashboard</p>
                                            </div>
                                        </div>";

                                        await _emailService.SendEmailAsync(
                                                player.playerEmail!,
                                                player.FullName,
                                                subject,
                                                html
                                        );
                                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task update email for task {TaskId}", id);
        }

        return updated;
    }

    private async System.Threading.Tasks.Task SendTaskCompletionEmailAsync(Task task)
    {
        // Get scout details
        var scout = await GetScoutByIdAsync(task.AssignedToScoutId);
        if (scout?.Email == null)
        {
            _logger.LogWarning("Scout {ScoutId} has no email, skipping task completion notification", task.AssignedToScoutId);
            return;
        }

        // If there's a player assigned, send email to scout with player name
        if (!string.IsNullOrEmpty(task.PlayerId))
        {
            var player = await GetPlayerByIdAsync(task.PlayerId);
            if (player != null)
            {
                var playerName = player.FullName;
                await _emailService.SendTaskCompletedAsync(
                    scout.Email,
                    scout.ScoutName,
                    playerName,
                    task.Title
                );
            }
        }
        else
        {
            // If no specific player, just send with generic message
            await _emailService.SendTaskCompletedAsync(
                scout.Email,
                scout.ScoutName,
                "A player",
                task.Title
            );
        }
    }

    private async System.Threading.Tasks.Task<Scout?> GetScoutByIdAsync(string scoutId)
    {
        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.scouts WHERE scout_id = @p_id",
            MapReaderToScout,
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = scoutId }
        );
    }

    private Scout? MapReaderToScout(NpgsqlDataReader reader)
    {
        try
        {
            return new Scout
            {
                ScoutId = reader["scout_id"].ToString()!,
                ScoutName = reader["scout_name"].ToString()!,
                RoleName = reader["role_name"].ToString()!,
                FirstName = reader["first_name"] == DBNull.Value ? null : reader["first_name"].ToString(),
                LastName = reader["last_name"] == DBNull.Value ? null : reader["last_name"].ToString(),
                Email = reader["email"] == DBNull.Value ? null : reader["email"].ToString(),
                PhoneNumber = reader["phone_number"] == DBNull.Value ? null : reader["phone_number"].ToString(),
                AddressLine1 = reader["address_line1"] == DBNull.Value ? null : reader["address_line1"].ToString(),
                AddressLine2 = reader["address_line2"] == DBNull.Value ? null : reader["address_line2"].ToString(),
                City = reader["city"] == DBNull.Value ? null : reader["city"].ToString(),
                State = reader["state"] == DBNull.Value ? null : reader["state"].ToString(),
                PostalCode = reader["postal_code"] == DBNull.Value ? null : reader["postal_code"].ToString(),
                Country = reader["country"] == DBNull.Value ? null : reader["country"].ToString(),
                CreatedAt = (DateTime)reader["created_at"]
            };
        }
        catch
        {
            return null;
        }
    }

    private async System.Threading.Tasks.Task<Player1?> GetPlayerByIdAsync(string playerId)
    {
        return await _db.ExecuteQuerySingleAsync(
            "SELECT * FROM stf.players WHERE player_id = @p_id",
            MapReaderToPlayer,
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = playerId }
        );
    }

    private Player1? MapReaderToPlayer(NpgsqlDataReader reader)
    {
        try
        {
            return new Player1
            {
                PlayerId = reader["player_id"].ToString()!,
                FullName = reader["full_name"].ToString()!,
                DateOfBirth = reader["date_of_birth"] == DBNull.Value ? default : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("date_of_birth"))),
                Nationality = reader["nationality"].ToString()!,
                PositionCode = reader["position_code"].ToString()!,
                PreferredFoot = reader["preferred_foot"].ToString()!,
                HeightCm = (int)reader["height_cm"],
                WeightKg = (int)reader["weight_kg"],
                CurrentClubId = reader["current_club_id"] == DBNull.Value ? null : reader["current_club_id"].ToString(),
                ContractStartDate = reader["contract_start_date"] == DBNull.Value ? default : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("contract_start_date"))),
                ContractEndDate = reader["contract_end_date"] == DBNull.Value ? default : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("contract_end_date"))),
                AgentName = reader["agent_name"].ToString()!,
                playerEmail = reader["player_email"].ToString()!,
                AgentScoutId = reader["agent_scout_id"].ToString()!,
                ContactInfo = reader["contact_info"] == DBNull.Value ? null : reader["contact_info"].ToString(),
                ProfileImageUrl = reader["profile_image_url"] == DBNull.Value ? null : reader["profile_image_url"].ToString(),
                CreatedAt = (DateTime)reader["created_at"],
                UpdatedAt = (DateTime)reader["updated_at"]
            };
        }
        catch
        {
            return null;
        }
    }

    public async System.Threading.Tasks.Task SendConsolidatedFollowupEmailAsync(string reviewId)
    {
        // Find all tasks created from this review by matching source='review' and description containing the Review ID
        var tasks = await _db.ExecuteQueryListAsync<Task>(
            "SELECT task_id, title, description, player_id, due_date FROM stf.tasks WHERE source = 'review' AND description ILIKE @p_pattern ORDER BY created_at",
            reader => new Task
            {
                TaskId = reader["task_id"].ToString()!,
                Title = reader["title"].ToString()!,
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                PlayerId = reader["player_id"] == DBNull.Value ? null : reader["player_id"].ToString(),
                DueDate = reader["due_date"] == DBNull.Value ? default : DateOnly.FromDateTime((DateTime)reader["due_date"]),
            },
            new NpgsqlParameter("p_pattern", NpgsqlDbType.Varchar) { Value = $"%Review ID: {reviewId}%" }
        );

        if (tasks.Count == 0)
        {
            _logger.LogInformation("No review follow-up tasks found for review {ReviewId}", reviewId);
            return;
        }

        // Determine recipient: prefer player email if available
        string? playerId = tasks.FirstOrDefault(t => !string.IsNullOrEmpty(t.PlayerId))?.PlayerId;
        string? recipientEmail = null;
        string recipientName = "";
        if (!string.IsNullOrEmpty(playerId))
        {
            var player = await GetPlayerByIdAsync(playerId!);
            if (player != null && !string.IsNullOrEmpty(player.playerEmail))
            {
                recipientEmail = player.playerEmail;
                recipientName = player.FullName;
            }
        }

        // Fallback: try to parse scout email from tasks' assigned scout info (not stored here), so skip fallback for now
        if (string.IsNullOrEmpty(recipientEmail))
        {
            _logger.LogWarning("No player email found for review {ReviewId} tasks; skipping consolidated email.", reviewId);
            return;
        }

        // Build HTML table
        var rows = new System.Text.StringBuilder();
        foreach (var t in tasks)
        {
            var taskUrl = $"{GetBaseUrl()}#/tasks?taskId={Uri.EscapeDataString(t.TaskId)}";
            rows.Append($"<tr>");
            rows.Append($"<td style='padding:10px; border:1px solid #e3e8ee;'>{EscapeHtml(t.Title)}</td>");
            rows.Append($"<td style='padding:10px; border:1px solid #e3e8ee;'>{EscapeHtml(t.DueDate.ToString())}</td>");
            rows.Append($"<td style='padding:10px; border:1px solid #e3e8ee;'>{EscapeHtml(t.Description)}</td>");
            rows.Append($"<td style='padding:10px; border:1px solid #e3e8ee; text-align:center;'><a href='{taskUrl}' style='background:#1f4e79; color:#fff; padding:6px 12px; text-decoration:none; border-radius:4px; font-size:12px; display:inline-block;'>View Task</a></td>");
            rows.Append($"</tr>");
        }

        var html = $@"<div style='font-family: Arial, Helvetica, sans-serif; color:#111;'><p>Hi {EscapeHtml(recipientName)},</p><p>The following follow-up tasks were created from review {EscapeHtml(reviewId)}:</p><table style='border-collapse:collapse; width:100%;'><thead><tr><th style='padding:10px; border:1px solid #e3e8ee; text-align:left;'>Task</th><th style='padding:10px; border:1px solid #e3e8ee; text-align:left;'>Due Date</th><th style='padding:10px; border:1px solid #e3e8ee; text-align:left;'>Description</th><th style='padding:10px; border:1px solid #e3e8ee; text-align:left;'>Link</th></tr></thead><tbody>{rows}</tbody></table><p>Please open the task(s) using the links above.</p></div>";

        try
        {
            await _emailService.SendEmailAsync(recipientEmail!, recipientName, $"Review follow-up tasks for review {reviewId}", html);
            _logger.LogInformation("Sent consolidated follow-up email for review {ReviewId} to {Recipient}", reviewId, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send consolidated follow-up email for review {ReviewId}", reviewId);
        }
    }

    private string GetBaseUrl()
    {
        // Prefer configured base URL if available; fall back to localhost origin
        // This method may be improved to read from config or request context.
        return "https://localhost:7001";
    }

    private static string EscapeHtml(string? value)
    {
        if (value == null) return string.Empty;
        return System.Net.WebUtility.HtmlEncode(value);
    }

    public async Task<bool> DeleteTaskAsync(string id)
    {
        var result = await _db.ExecuteScalarAsync(
            "SELECT stf.fn_tasks_delete(@p_id)",
            new NpgsqlParameter("p_id", NpgsqlDbType.Varchar) { Value = id }
        );
        return Convert.ToInt32(result ?? 0) > 0;
    }

    private Task MapReaderToTask(NpgsqlDataReader reader)
    {
        return new Task
        {
            TaskId = reader["task_id"].ToString()!,
            Title = reader["title"].ToString()!,
            Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
            PlayerId = reader["player_id"] == DBNull.Value ? null : reader["player_id"].ToString(),
            ClubId = reader["club_id"] == DBNull.Value ? null : reader["club_id"].ToString(),
            AssignedToScoutId = reader["assigned_to_scout_id"].ToString()!,
            DueDate = reader["due_date"] == DBNull.Value ? default : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("due_date"))),
            Status = reader["status"].ToString()!,
            Source = reader["source"].ToString()!,
            CreatedAt = (DateTime)reader["created_at"]
        };
    }
}
