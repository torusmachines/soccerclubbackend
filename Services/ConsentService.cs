using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Services;

public class ConsentService : IConsentService
{
    private readonly AppDbContext _appDbContext;
    private readonly IConfiguration _configuration;

    public ConsentService(AppDbContext appDbContext, IConfiguration configuration)
    {
        _appDbContext = appDbContext;
        _configuration = configuration;
    }

    public string CurrentPolicyVersion => _configuration["PrivacyPolicy:CurrentVersion"] ?? "v1.0";

    public async System.Threading.Tasks.Task RecordConsentAsync(string userId, bool consentGiven, string consentVersion, string consentSource)
    {
        var entry = new UserConsentHistory
        {
            UserId = userId,
            ConsentGiven = consentGiven,
            ConsentVersion = consentVersion,
            ConsentSource = consentSource,
            CreatedAt = DateTime.UtcNow
        };

        _appDbContext.UserConsentHistories.Add(entry);
        await _appDbContext.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task GrantConsentAsync(ApplicationUser user, string consentSource)
    {
        user.ConsentGiven = true;
        user.ConsentGivenAt = DateTime.UtcNow;
        user.ConsentVersion = CurrentPolicyVersion;
        user.IsActive = true;

        await RecordConsentAsync(user.Id, true, CurrentPolicyVersion, consentSource);
    }

    public async System.Threading.Tasks.Task WithdrawConsentAsync(ApplicationUser user, string consentSource)
    {
        user.ConsentGiven = false;
        user.IsActive = false;

        await RecordConsentAsync(user.Id, false, user.ConsentVersion ?? CurrentPolicyVersion, consentSource);
    }

    public async System.Threading.Tasks.Task EnsurePolicyVersionAlignmentAsync(ApplicationUser user)
    {
        if (user.ConsentVersion == CurrentPolicyVersion)
        {
            return;
        }

        user.ConsentGiven = false;
        user.ConsentVersion = CurrentPolicyVersion;

        var alreadyRecorded = await _appDbContext.UserConsentHistories
            .AnyAsync(x => x.UserId == user.Id
                && x.ConsentSource == "policy-update-required"
                && x.ConsentVersion == CurrentPolicyVersion);

        if (!alreadyRecorded)
        {
            await RecordConsentAsync(user.Id, false, CurrentPolicyVersion, "policy-update-required");
        }
    }

    public async System.Threading.Tasks.Task<List<UserConsentHistory>> GetHistoryAsync(string userId)
    {
        return await _appDbContext.UserConsentHistories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
