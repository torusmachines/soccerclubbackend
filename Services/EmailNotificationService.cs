using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FootballDashboardAPI.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IConfiguration config,
        ILogger<EmailNotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Core send method ─────────────────────────────────────────────
    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        try
        {
            ValidatePowerAutomateConfiguration();
            await SendEmailViaPowerAutomateAsync(toEmail, subject, htmlContent);

            _logger.LogInformation("✅ Email sent to {Email} - {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private string PowerAutomateFlowUrl => _config["PowerAutomate:FlowUrl"] ?? string.Empty;

    private async Task SendEmailViaPowerAutomateAsync(string toEmail, string subject, string htmlContent)
    {
        using var client = new HttpClient();

        var payload = new
        {
            to = toEmail,
            subject,
            body = htmlContent
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(PowerAutomateFlowUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Power Automate email request failed ({response.StatusCode}): {responseBody}");
        }
    }

    private void ValidatePowerAutomateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(PowerAutomateFlowUrl))
            throw new InvalidOperationException("Power Automate flow URL is not configured. Set PowerAutomate:FlowUrl in appsettings or environment variables.");
    }

    // ── Contract expiry alert ─────────────────────────────────────────
    public async Task SendContractExpiryAlertAsync(
        string toEmail, string playerName, string contractEndDate, string scoutName)
    {
        var subject = $"⚠️ Contract Expiring Soon — {playerName}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px;'>
            <div style='background: #e74c3c; padding: 15px; border-radius: 8px 8px 0 0;'>
                <h2 style='color: white; margin: 0;'>⚠️ Contract Expiry Alert</h2>
            </div>
            <div style='border: 1px solid #ddd; padding: 20px; border-radius: 0 0 8px 8px;'>
                <p>Hi <strong>{scoutName}</strong>,</p>
                <p>The contract for player <strong>{playerName}</strong> 
                   is expiring on <strong style='color:#e74c3c;'>{contractEndDate}</strong>.</p>
                <p>Please begin renewal discussions as soon as possible.</p>
                <br/>
                <p style='color: #888; font-size: 12px;'>— Football Scout Dashboard</p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, scoutName, subject, html);
    }

    // ── Task due alert ────────────────────────────────────────────────
    public async Task SendTaskDueAlertAsync(
        string toEmail, string taskTitle, string dueDate, string assignedTo)
    {
        var subject = $"📋 Task Due Soon — {taskTitle}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px;'>
            <div style='background: #f39c12; padding: 15px; border-radius: 8px 8px 0 0;'>
                <h2 style='color: white; margin: 0;'>📋 Task Due Alert</h2>
            </div>
            <div style='border: 1px solid #ddd; padding: 20px; border-radius: 0 0 8px 8px;'>
                <p>Hi <strong>{assignedTo}</strong>,</p>
                <p>The following task is due on <strong style='color:#f39c12;'>{dueDate}</strong>:</p>
                <div style='background:#fff8e1; padding:12px; border-left:4px solid #f39c12; margin:10px 0;'>
                    <strong>{taskTitle}</strong>
                </div>
                <p>Please make sure to complete it on time.</p>
                <br/>
                <p style='color: #888; font-size: 12px;'>— Football Scout Dashboard</p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, assignedTo, subject, html);
    }

    // ── Review follow-up reminder ─────────────────────────────────────
    public async Task SendReviewFollowUpAsync(
        string toEmail, string playerName, string skillKey, string followUpDate, string scoutName)
    {
        var subject = $"🔍 Review Follow-Up — {playerName} ({skillKey})";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px;'>
            <div style='background: #3498db; padding: 15px; border-radius: 8px 8px 0 0;'>
                <h2 style='color: white; margin: 0;'>🔍 Review Follow-Up Reminder</h2>
            </div>
            <div style='border: 1px solid #ddd; padding: 20px; border-radius: 0 0 8px 8px;'>
                <p>Hi <strong>{scoutName}</strong>,</p>
                <p>You have a follow-up scheduled for 
                   <strong style='color:#3498db;'>{followUpDate}</strong>:</p>
                <ul>
                    <li>Player: <strong>{playerName}</strong></li>
                    <li>Skill: <strong>{skillKey}</strong></li>
                </ul>
                <p>Please log in to the dashboard to update your notes.</p>
                <br/>
                <p style='color: #888; font-size: 12px;'>— Football Scout Dashboard</p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, scoutName, subject, html);
    }

    // ── Task assigned to player ───────────────────────────────────────
    public async Task SendTaskAssignedAsync(
        string toEmail, string playerName, string taskTitle, string dueDate, string scoutName)
    {
        var subject = $"✅ New Task Assigned — {taskTitle}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px;'>
            <div style='background: #27ae60; padding: 15px; border-radius: 8px 8px 0 0;'>
                <h2 style='color: white; margin: 0;'>✅ New Task Assigned</h2>
            </div>
            <div style='border: 1px solid #ddd; padding: 20px; border-radius: 0 0 8px 8px;'>
                <p>Hi <strong>{playerName}</strong>,</p>
                <p><strong>{scoutName}</strong> has assigned you a new task:</p>
                <div style='background:#e8f8f5; padding:12px; border-left:4px solid #27ae60; margin:10px 0;'>
                    <strong>{taskTitle}</strong>
                </div>
                <p>Due date: <strong style='color:#27ae60;'>{dueDate}</strong></p>
                <p>Please complete this task by the due date.</p>
                <br/>
                <p style='color: #888; font-size: 12px;'>— Football Scout Dashboard</p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, playerName, subject, html);
    }

    // ── Task completed by player ──────────────────────────────────────
    public async Task SendTaskCompletedAsync(
        string toEmail, string scoutName, string playerName, string taskTitle)
    {
        var subject = $"🎉 Task Completed — {playerName}";
        var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px;'>
            <div style='background: #2980b9; padding: 15px; border-radius: 8px 8px 0 0;'>
                <h2 style='color: white; margin: 0;'>🎉 Task Completed</h2>
            </div>
            <div style='border: 1px solid #ddd; padding: 20px; border-radius: 0 0 8px 8px;'>
                <p>Hi <strong>{scoutName}</strong>,</p>
                <p>Great news! <strong>{playerName}</strong> has completed the following task:</p>
                <div style='background:#ebf5fb; padding:12px; border-left:4px solid #2980b9; margin:10px 0;'>
                    <strong>{taskTitle}</strong>
                </div>
                <p>Please log in to the dashboard to review and provide feedback if needed.</p>
                <br/>
                <p style='color: #888; font-size: 12px;'>— Football Scout Dashboard</p>
            </div>
        </div>";

        await SendEmailAsync(toEmail, scoutName, subject, html);
    }
}