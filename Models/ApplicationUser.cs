using Microsoft.AspNetCore.Identity;

namespace FootballDashboardAPI.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Admin | Player | Scout | Coach</summary>
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = false;

    public bool ConsentGiven { get; set; } = false;

    public DateTime? ConsentGivenAt { get; set; }

    public string? ConsentVersion { get; set; } = "v1.0";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? InviteToken { get; set; }

    public DateTime? InviteTokenExpiry { get; set; }

    public bool IsInviteAccepted { get; set; } = false;

    /// <summary>Pending | Approved | Rejected</summary>
    public string UserStatus { get; set; } = "Pending";

    /// <summary>Role chosen at self-signup: Player | Coach</summary>
    public string? SignupRole { get; set; }

    public ICollection<UserConsentHistory> ConsentHistory { get; set; } = new List<UserConsentHistory>();
}
