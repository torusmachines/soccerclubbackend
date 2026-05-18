using FootballDashboardAPI.Models;
using FootballDashboardAPI.Models.Responses;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task = FootballDashboardAPI.Models.Task;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly FootballContext _footballContext;
    private readonly FootballDashboardAPI.Data.AppDbContext _appDbContext;

    public TasksController(ITaskService taskService, FootballContext footballContext, FootballDashboardAPI.Data.AppDbContext appDbContext)
    {
        _taskService = taskService;
        _footballContext = footballContext;
        _appDbContext = appDbContext;
    }

    [HttpPost("send-followup/{reviewId}")]
    public async Task<IActionResult> SendReviewFollowupEmail(string reviewId)
    {
        await _taskService.SendConsolidatedFollowupEmailAsync(reviewId);
        return Ok(new { message = "Consolidated follow-up email triggered." });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Task>>> GetTasks()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    [HttpGet("configration")]
    public async Task<ActionResult<TaskConfigrationResponse>> GetTaskConfigration()
    {
        var clubs = await _footballContext.Clubs
            .AsNoTracking()
            .OrderBy(c => c.ClubName)
            .Select(c => new TaskClubOptionResponse
            {
                ClubId = c.ClubId,
                ClubName = c.ClubName
            })
            .ToListAsync();

        var scouts = await _footballContext.Scouts
            .AsNoTracking()
            .OrderBy(s => s.ScoutName)
            .Select(s => new TaskScoutOptionResponse
            {
                ScoutId = s.ScoutId,
                ScoutName = s.ScoutName
            })
            .ToListAsync();

        var players = await _footballContext.Players1
            .AsNoTracking()
            .Include(p => p.Sport)
            .Where(p => p.UserStatus == "Approved")
            .OrderBy(p => p.FullName)
            .Select(p => new TaskPlayerOptionResponse
            {
                PlayerId = p.PlayerId,
                PlayerName = p.FullName,
                SportId = p.SportId,
                SportName = p.Sport != null ? p.Sport.SportName : string.Empty
            })
            .ToListAsync();

        return Ok(new TaskConfigrationResponse
        {
            allClubsForTask = clubs,
            allScoutForTask = scouts,
            allPlayerForTask = players
        });
    }

    [HttpGet("page")]
    public async Task<ActionResult<IEnumerable<TaskPageResponse>>> GetTasksForPage(
        [FromQuery(Name = "status")] string? status = null,
        [FromQuery(Name = "scoutId")] string? scoutId = null,
        [FromQuery(Name = "upcomingDays")] int? upcomingDays = null,
        [FromQuery(Name = "search")] string? search = null,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10)
    {
        var query = _footballContext.Tasks
            .AsNoTracking()
            .Include(t => t.Player)
            .Include(t => t.Club)
            .AsQueryable();

        // Apply status filter (case-insensitive)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusLower = status.ToLower();
            query = statusLower switch
            {
                "all" => query,
                "open" => query.Where(t => EF.Functions.ILike(t.Status, "open")),
                "closed" => query.Where(t => EF.Functions.ILike(t.Status, "closed")),
                "overdue" => query.Where(t => t.DueDate < today && EF.Functions.ILike(t.Status, "open")),
                "upcoming" => query.Where(t => t.DueDate >= today && t.DueDate <= today.AddDays(2) && EF.Functions.ILike(t.Status, "open")),
                _ => query.Where(t => EF.Functions.ILike(t.Status, "open"))
            };
        }

        // Apply scout filter
        if (!string.IsNullOrWhiteSpace(scoutId))
        {
            query = query.Where(t => t.AssignedToScoutId == scoutId);
        }

        // Apply upcoming days filter
        if (upcomingDays.HasValue && upcomingDays.Value > 0)
        {
            var upcomingDate = today.AddDays(upcomingDays.Value);
            query = query.Where(t => t.DueDate >= today && t.DueDate <= upcomingDate && EF.Functions.ILike(t.Status, "open"));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Title, searchTerm) ||
                EF.Functions.ILike(t.Description ?? string.Empty, searchTerm));
        }

        // Apply pagination
        var totalCount = await query.CountAsync();
        var rawTasks = await query
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TaskId,
                t.Title,
                Description = t.Description ?? string.Empty,
                AssignedById = t.AssignedToScoutId,
                AssignedTo = t.Player != null ? t.Player.FullName : (t.Club != null ? t.Club.ClubName : "Unassigned"),
                AssignedToId = t.PlayerId ?? t.ClubId ?? string.Empty,
                AssignedToType = t.Player != null ? "player" : (t.Club != null ? "club" : ""),
                t.CreatedAt,
                t.Source,
                Status = CalculateStatus(t.DueDate, t.Status),
                StatusFromTable = t.Status,
                t.DueDate
            })
            .ToListAsync();

        // Resolve AssignedBy values with fallbacks: Scouts -> AspNetUserRoles.RoleId -> AspNetRoles.Name
        var assignedIds = rawTasks
            .Select(r => r.AssignedById)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var scoutsMap = new Dictionary<string, string>();
        if (assignedIds.Count > 0)
        {
            scoutsMap = await _footballContext.Scouts
                .AsNoTracking()
                .Where(s => assignedIds.Contains(s.ScoutId))
                .ToDictionaryAsync(s => s.ScoutId, s => s.ScoutName);
        }

        // Determine which assignedIds still need lookup in identity tables
        var remainingIds = assignedIds.Where(id => !scoutsMap.ContainsKey(id)).ToList();
        var userRoleMap = new Dictionary<string, string>(); // userId -> roleName
        if (remainingIds.Count > 0)
        {
            // fetch user-role pairs
            var userRoles = await _appDbContext.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
                .AsNoTracking()
                .Where(ur => remainingIds.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.RoleId })
                .ToListAsync();

            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
            var roles = new Dictionary<string, string>();
            if (roleIds.Count > 0)
            {
                roles = await _appDbContext.Roles
                    .AsNoTracking()
                    .Where(r => roleIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Name);
            }

            // map userId -> first matching role name
            foreach (var ur in userRoles)
            {
                if (!userRoleMap.ContainsKey(ur.UserId))
                {
                    roles.TryGetValue(ur.RoleId, out var roleName);
                    userRoleMap[ur.UserId] = roleName; // roleName may be null
                }
            }
        }

        var tasks = rawTasks.Select(r => new TaskPageResponse
        {
            TaskId = r.TaskId,
            Title = r.Title,
            Description = r.Description,
            AssignedBy = string.IsNullOrWhiteSpace(r.AssignedById)
                ? "Auto-generated"
                : (scoutsMap.TryGetValue(r.AssignedById, out var sname)
                    ? sname
                    : (userRoleMap.TryGetValue(r.AssignedById, out var rname) ? rname : null)),
            AssignedById = r.AssignedById ?? string.Empty,
            AssignedTo = r.AssignedTo,
            AssignedToId = r.AssignedToId,
            AssignedToType = r.AssignedToType,
            CreatedAt = r.CreatedAt,
            Source = r.Source,
            Status = r.Status,
            StatusFromTable = r.StatusFromTable,
            DueDate = r.DueDate
        }).ToList();

        // Return with pagination metadata in headers
        Response.Headers["X-Total-Count"] = totalCount.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-PageSize"] = pageSize.ToString();

        return Ok(tasks);
    }

    private static string CalculateStatus(DateOnly dueDate, string dbStatus)
    {
        if (dbStatus.Equals("closed", StringComparison.OrdinalIgnoreCase))
            return "Completed";

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dueDate < today)
            return "Overdue";

        var daysRemaining = (dueDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
        if (daysRemaining >= 2)
            return "Upcoming";

        return "Open";
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Task>> GetTask(string id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);

        if (task == null)
            return NotFound(new { message = $"Task with ID '{id}' not found." });

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<Task>> CreateTask(CreateTask dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Request body is required." });

        var task = await _taskService.CreateTaskAsync(dto);

        return CreatedAtAction(nameof(GetTask), new { id = task.TaskId }, task);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Task>> UpdateTask(string id, UpdateTask dto)
    {
        var task = await _taskService.UpdateTaskAsync(id, dto);

        if (task == null)
            return NotFound(new { message = $"Task with ID '{id}' not found." });

        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        var result = await _taskService.DeleteTaskAsync(id);

        if (!result)
            return NotFound(new { message = $"Task with ID '{id}' not found." });

        return NoContent();
    }
}