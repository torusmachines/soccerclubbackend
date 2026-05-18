using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IConsentService
{
    string CurrentPolicyVersion { get; }
    System.Threading.Tasks.Task RecordConsentAsync(string userId, bool consentGiven, string consentVersion, string consentSource);
    System.Threading.Tasks.Task GrantConsentAsync(ApplicationUser user, string consentSource);
    System.Threading.Tasks.Task WithdrawConsentAsync(ApplicationUser user, string consentSource);
    System.Threading.Tasks.Task EnsurePolicyVersionAlignmentAsync(ApplicationUser user);
    System.Threading.Tasks.Task<List<UserConsentHistory>> GetHistoryAsync(string userId);
}
