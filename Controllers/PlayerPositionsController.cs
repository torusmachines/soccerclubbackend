using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PlayerPositionsController : ControllerBase
{
    private readonly IPlayerPositionService _service;

    public PlayerPositionsController(IPlayerPositionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerPosition>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlayerPosition>> Get(string id)
    {
        var item = await _service.GetByIdAsync(id);

        if (item == null)
            return NotFound(new { message = "Player position not found" });

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerPosition>> Create(CreatePlayerPosition dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var item = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(Get), new { id = item.PositionId }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerPosition>> Update(string id, UpdatePlayerPosition dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);

            if (item == null)
                return NotFound(new { message = "Player position not found" });

            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Player position not found" });

            return Ok(new { message = "Player position deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
