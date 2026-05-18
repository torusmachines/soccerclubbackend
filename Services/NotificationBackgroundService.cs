using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

// Alias to avoid conflict with FootballDashboardAPI.Models.Task
using SystemTask = System.Threading.Tasks.Task;

namespace FootballDashboardAPI.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider services,
        ILogger<NotificationBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

        protected override async SystemTask ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running daily notification check: {Time}", DateTime.UtcNow);
                await CheckAndSendNotifications();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification check failed");
            }

            await System.Threading.Tasks.Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async SystemTask CheckAndSendNotifications()
    {
        using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FootballContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 2. Task due alerts (due within 3 days) ───────────────────
        // ✅ Fix: use context.Set<Models.Task>() to avoid ambiguity
        // --- Tasks due soon (within 3 days) ---
        var dueTasks = await context.Tasks
            .Include(t => t.AssignedToScout)
            .Where(t => t.Status == "open" && t.DueDate <= today.AddDays(3) && t.DueDate >= today)
            .ToListAsync();

        _logger.LogInformation("Found {Count} tasks due soon", dueTasks.Count);

        foreach (var task in dueTasks)
        {
            if (task.AssignedToScout == null) continue;

            var scoutEmail = task.AssignedToScout.Email;
            if (string.IsNullOrEmpty(scoutEmail)) continue;

            await emailService.SendTaskDueAlertAsync(
                toEmail: scoutEmail,
                taskTitle: task.Title,
                dueDate: task.DueDate.ToString(),
                assignedTo: task.AssignedToScout.ScoutName
            );
        }

        

        // ── 3. Review follow-up reminders (due within 3 days) ────────
        var followUps = await context.Set<ReviewSkillDetail>()
            .Include(r => r.Review)
                .ThenInclude(r => r.Scout)
            .Include(r => r.Review)
                .ThenInclude(r => r.Player)
            .Where(r => r.FollowUpDate != null &&
                        r.FollowUpDate <= today.AddDays(3) &&
                        r.FollowUpDate >= today)
            .ToListAsync();

        _logger.LogInformation("Found {Count} review follow-ups", followUps.Count);

        foreach (var detail in followUps)
        {
            if (detail.Review?.Scout == null) continue;

            var scoutEmail = detail.Review.Scout?.Email;
            if (string.IsNullOrEmpty(scoutEmail)) continue;

            await emailService.SendReviewFollowUpAsync(
                toEmail: scoutEmail,
                playerName: detail.Review.Player?.FullName ?? "Unknown",
                skillKey: detail.SkillKey,
                followUpDate: detail.FollowUpDate.ToString()!,
                scoutName: detail.Review.Scout.ScoutName
            );
        }
    }
}