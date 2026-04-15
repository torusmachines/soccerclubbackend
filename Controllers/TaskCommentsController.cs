using System.Security.Claims;
using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class TaskCommentsController : ControllerBase
{
    private readonly ITaskCommentService _commentService;

    public TaskCommentsController(ITaskCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("tasks/{taskId}/comments")]
    public async Task<ActionResult<IEnumerable<TaskComment>>> GetCommentsByTask(
        string taskId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 20;

        var comments = await _commentService.GetCommentsByTaskAsync(taskId, page, pageSize);
        return Ok(comments);
    }

    [HttpPost("tasks/{taskId}/comments")]
    public async Task<ActionResult<TaskComment>> AddComment(string taskId, [FromBody] CreateCommentRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { message = "Comment text is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Unable to resolve user identity." });

        var created = await _commentService.CreateCommentAsync(taskId, userId, request.Comment.Trim(), request.IsVisibleToPlayer);
        if (created == null)
            return StatusCode(500, new { message = "Unable to create comment." });

        return CreatedAtAction(nameof(GetCommentsByTask), new { taskId }, created);
    }

    [HttpPut("comments/{commentId}")]
    public async Task<ActionResult<TaskComment>> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { message = "Comment text is required." });

        var updated = await _commentService.UpdateCommentAsync(commentId, request.Comment.Trim(), request.IsVisibleToPlayer);
        if (updated == null)
            return NotFound(new { message = "Comment not found or cannot be updated." });

        return Ok(updated);
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var deleted = await _commentService.DeleteCommentAsync(commentId);
        if (!deleted)
            return NotFound(new { message = "Comment not found." });

        return NoContent();
    }
}

public class CreateCommentRequest
{
    public string Comment { get; set; } = null!;
    public bool IsVisibleToPlayer { get; set; } = true;
}

public class UpdateCommentRequest
{
    public string Comment { get; set; } = null!;
    public bool IsVisibleToPlayer { get; set; } = true;
}
