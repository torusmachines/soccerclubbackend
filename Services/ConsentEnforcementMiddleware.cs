using FootballDashboardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FootballDashboardAPI.Services;

public class ConsentEnforcementMiddleware
{
    private static readonly HashSet<string> BypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/signup",
        "/api/auth/signup/player",
        "/api/auth/signup/coach",
        "/api/auth/accept-invite",
        "/api/auth/invite",
        "/api/auth/invite-user",
        "/api/auth/me",
        "/api/consent/re-consent"
    };

    private readonly RequestDelegate _next;

    public ConsentEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async System.Threading.Tasks.Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, IConsentService consentService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassPaths.Contains(path) || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Authentication is required." });
            return;
        }

        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Authentication is required." });
            return;
        }

        await consentService.EnsurePolicyVersionAlignmentAsync(user);
        await userManager.UpdateAsync(user);

        if (!user.ConsentGiven || !user.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Consent is required to access this resource.",
                code = "CONSENT_REQUIRED",
                consentGiven = user.ConsentGiven,
                isActive = user.IsActive,
                requiredConsentVersion = consentService.CurrentPolicyVersion
            });
            return;
        }

        await _next(context);
    }
}
