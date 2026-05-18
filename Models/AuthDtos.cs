using System.ComponentModel.DataAnnotations;

namespace FootballDashboardAPI.Models;

// ── Invite User ──────────────────────────────────────────────────────────────

public class InviteUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Player|Scout|Coach)$", ErrorMessage = "Role must be Admin, Player, Scout, or Coach")]
    public string Role { get; set; } = string.Empty;
}

// ── Player Self-Signup ───────────────────────────────────────────────────────

public class PlayerSignupRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters with uppercase, lowercase, number, and special character.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Consent is required.")]
    public bool ConsentGiven { get; set; }

    // Player-specific fields
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
    public string? CurrentClub { get; set; }
    public string? ContractStatus { get; set; }
    public string? AgentName { get; set; }
    public string? ContactInfo { get; set; }
    public string? ProfileImage { get; set; }
    public int? SportId { get; set; }

    public string? Pincode { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
}

// ── Coach Self-Signup ────────────────────────────────────────────────────────

public class CoachSignupRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters with uppercase, lowercase, number, and special character.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Consent is required.")]
    public bool ConsentGiven { get; set; }

    // Coach/Scout-specific fields
    public string? ScoutId { get; set; }
    public string? RoleName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    public string? Pincode { get; set; }
    public string? Country { get; set; }
    public int? SportId { get; set; }
    public string? ProfileImage { get; set; }
}

public class SelfSignupRequest
{
    [Required]
    [RegularExpression("^(Player|Scout)$", ErrorMessage = "signupRole must be Player or Scout")]
    public string SignupRole { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters with uppercase, lowercase, number, and special character.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Consent is required.")]
    public bool ConsentGiven { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
    public string? CurrentClub { get; set; }
    public string? ContractStatus { get; set; }
    public string? AgentName { get; set; }
    public string? ContactInfo { get; set; }
    public string? ProfileImage { get; set; }
    public int? SportId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? RoleName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    public string? Pincode { get; set; }
    public string? Country { get; set; }
}

// ── Admin Approve/Reject ─────────────────────────────────────────────────────

public class ApproveRejectRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Action must be Approved or Rejected")]
    public string Action { get; set; } = string.Empty;
}

// ── Pending User DTO ─────────────────────────────────────────────────────────

public class PendingUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SignupRole { get; set; } = string.Empty;
    public string UserStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Accept Invite ────────────────────────────────────────────────────────────

public class AcceptInviteRequest
{
    [Required]
    public string InviteToken { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Consent is required.")]
    public bool ConsentGiven { get; set; }
}

// ── Login ────────────────────────────────────────────────────────────────────

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// ── Responses ────────────────────────────────────────────────────────────────

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfoDto User { get; set; } = new();
    public bool RequiresConsent { get; set; }
    public string? RequiredConsentVersion { get; set; }
}

public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string? ScoutId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string UserStatus { get; set; } = string.Empty;
    public bool ConsentGiven { get; set; }
    public DateTime? ConsentGivenAt { get; set; }
    public string ConsentVersion { get; set; } = "v1.0";
    public bool IsActive { get; set; }
    public LoginUserDto? LoginUser { get; set; }
}

public class LoginUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g. "Player" or "Scout"
}

public class WithdrawConsentRequest
{
    [Required]
    public bool ConfirmWithdraw { get; set; }
}

public class ReConsentRequest
{
    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Consent is required.")]
    public bool ConsentGiven { get; set; }
}
