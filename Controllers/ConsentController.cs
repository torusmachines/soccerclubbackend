using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/consent")]
[Authorize]
public class ConsentController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConsentService _consentService;

    public ConsentController(UserManager<ApplicationUser> userManager, IConsentService consentService)
    {
        _userManager = userManager;
        _consentService = consentService;
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawConsentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConfirmWithdraw)
            return BadRequest(new { message = "Consent withdrawal must be explicitly confirmed." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        await _consentService.WithdrawConsentAsync(user, "withdraw");
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Consent withdrawn. Account has been deactivated." });
    }

    [HttpPost("re-consent")]
    public async Task<IActionResult> ReConsent([FromBody] ReConsentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        await _consentService.GrantConsentAsync(user, "reconsent");
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Consent updated successfully.", consentVersion = user.ConsentVersion });
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory([FromRoute] string userId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(requesterId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && !string.Equals(requesterId, userId, StringComparison.Ordinal))
            return Forbid();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var history = await _consentService.GetHistoryAsync(userId);
        return Ok(history.Select(x => new
        {
            x.Id,
            x.UserId,
            x.ConsentGiven,
            x.ConsentVersion,
            x.ConsentSource,
            x.CreatedAt
        }));
    }
}
