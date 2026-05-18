using System;
using Microsoft.AspNetCore.Mvc;
using FootballDashboardAPI.Services;
using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiPlanService _aiPlanService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiPlanService aiPlanService, ILogger<AiController> logger)
    {
        _aiPlanService = aiPlanService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new AI development plan for a player
    /// </summary>
    [HttpPost("generate/{playerId}")]
    public async Task<ActionResult<AiPlanResponse>> GenerateAiPlan(string playerId, [FromBody] AiPlanGenerateRequest? request)
    {
        try
        {
            _logger.LogInformation("Generating AI plan for player {PlayerId}", playerId);

            var result = await _aiPlanService.GenerateAiPlanAsync(playerId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid player ID: {PlayerId}", playerId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI plan for player {PlayerId}", playerId);

            // Detect AI provider/auth errors and return a 502 with a helpful message.
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("request failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("invalid api key", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(502, "AI provider error: authentication failed or provider returned an error. Check AI provider configuration and API key.");
            }

            return StatusCode(500, "An error occurred while generating the AI plan");
        }
    }

    /// <summary>
    /// Get the latest AI development plan for a player
    /// </summary>
    [HttpGet("latest/{playerId}")]
    public async Task<ActionResult<AiPlanResponse>> GetLatestAiPlan(string playerId)
    {
        try
        {
            var result = await _aiPlanService.GetLatestAiPlanAsync(playerId);

            if (result == null)
            {
                _logger.LogInformation("No latest AI plan found for player {PlayerId}, generating a new plan.", playerId);
                result = await _aiPlanService.GenerateAiPlanAsync(playerId);
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid or missing player for AI latest request: {PlayerId}", playerId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest AI plan for player {PlayerId}", playerId);
            return StatusCode(500, "An error occurred while retrieving the AI plan");
        }
    }

    /// <summary>
    /// Get the complete history of AI development plans for a player
    /// </summary>
    [HttpGet("history/{playerId}")]
    public async Task<ActionResult<AiPlanHistoryResponse>> GetAiPlanHistory(string playerId)
    {
        try
        {
            var result = await _aiPlanService.GetAiPlanHistoryAsync(playerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI plan history for player {PlayerId}", playerId);
            return StatusCode(500, "An error occurred while retrieving the AI plan history");
        }
    }
}