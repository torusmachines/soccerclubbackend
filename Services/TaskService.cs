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

        return await GetTaskByIdAsync(taskId) ?? new Task
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
    }

    public async Task<Task?> UpdateTaskAsync(string id, UpdateTask dto)
    {
        var existing = await GetTaskByIdAsync(id);
        if (existing == null)
            return null;

        // Determine new status
        var newStatus = dto.Status ?? existing.Status;
        var statusChanged = newStatus != existing.Status;
        var completedNow = statusChanged && newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);

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
