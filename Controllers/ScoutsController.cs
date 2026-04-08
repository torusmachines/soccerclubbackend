using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballDashboardAPI.Controllers;
[AllowAnonymous]
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ScoutsController : ControllerBase
{
    private readonly IScoutService _scoutService;
    private readonly ILogger<ScoutsController> _logger;

    public ScoutsController(IScoutService scoutService, ILogger<ScoutsController> logger)
    {
        _scoutService = scoutService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Scout>>> GetScouts()
    {
        var scouts = await _scoutService.GetAllScoutsAsync();
        return Ok(scouts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Scout>> GetScout(string id)
    {
        var scout = await _scoutService.GetScoutByIdAsync(id);
        
        if (scout == null)
        {
            return NotFound(new { message = $"Scout with ID '{id}' not found." });
        }

        return Ok(scout);
    }

    [HttpPost]
    public async Task<ActionResult<Scout>> CreateScout(CreateScout createScoutDto)
    {
        _logger.LogInformation("CreateScout API received lockedAreas={LockedAreas}", createScoutDto.LockedAreas);
        try
        {
            var scout = await _scoutService.CreateScoutAsync(createScoutDto);
            return CreatedAtAction(nameof(GetScout), new { id = scout.ScoutId }, scout);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Scout>> UpdateScout(string id, UpdateScout updateScoutDto)
    {
        _logger.LogInformation("UpdateScout API received id={Id} lockedAreas={LockedAreas}", id, updateScoutDto.LockedAreas);
        try
        {
            var scout = await _scoutService.UpdateScoutAsync(id, updateScoutDto);
            
            if (scout == null)
            {
                return NotFound(new { message = $"Scout with ID '{id}' not found." });
            }

            return Ok(scout);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScout(string id)
    {
        var result = await _scoutService.DeleteScoutAsync(id);
        
        if (!result)
        {
            return NotFound(new { message = $"Scout with ID '{id}' not found." });
        }

        return NoContent();
    }
}
