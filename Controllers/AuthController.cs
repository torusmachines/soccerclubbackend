using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailNotificationService _emailService;
    private readonly IConfiguration _config;
    private readonly FootballContext _footballContext;
    private readonly IConsentService _consentService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailNotificationService emailService,
        IConfiguration config,
        FootballContext footballContext,
        IConsentService consentService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _footballContext = footballContext;
        _consentService = consentService;
        _logger = logger;
        _emailService = emailService;
        _config = config;
    }

    [HttpPost("upload-signup-image")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadSignupImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension))
            return BadRequest(new { message = "Only JPG, JPEG, PNG, and WEBP images are allowed." });

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "signup-profile-images");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = $"signup-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var imageUrl = $"{Request.Scheme}://{Request.Host}/signup-profile-images/{fileName}";
        return Ok(new { imageUrl });
    }

    // ── POST /api/auth/invite ─────────────────────────────────────────────────
    [HttpPost("invite")]
    [HttpPost("invite-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check for duplicate email
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            if (existing.IsInviteAccepted)
                return Conflict(new { message = $"A user with email '{request.Email}' already exists." });

            // Re-invite an existing user whose invitation is still pending.
            existing.FullName = request.FullName;
            existing.Role = request.Role;
            existing.IsActive = false;
            existing.ConsentGiven = false;
            existing.ConsentGivenAt = null;
            existing.ConsentVersion = _consentService.CurrentPolicyVersion;
            existing.IsInviteAccepted = false;
            existing.InviteToken = GenerateInviteToken();
            existing.InviteTokenExpiry = DateTime.UtcNow.AddHours(48);
            existing.EmailConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(existing);
            if (!updateResult.Succeeded)
                return BadRequest(new { message = "Failed to update existing pending invite.", errors = updateResult.Errors });

            // Ensure Identity role membership matches requested role.
            var currentRoles = await _userManager.GetRolesAsync(existing);
            if (currentRoles.Any())
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(existing, currentRoles);
                if (!removeRolesResult.Succeeded)
                    return BadRequest(new { message = "Failed to update user role.", errors = removeRolesResult.Errors });
            }

            var addRoleResult = await _userManager.AddToRoleAsync(existing, request.Role);
            if (!addRoleResult.Succeeded)
                return BadRequest(new { message = "Failed to assign role to user.", errors = addRoleResult.Errors });

            await SendInviteEmailAsync(request.Email, request.FullName, request.Role, existing.InviteToken!);
            return Ok(new { message = $"Invitation resent to {request.Email}." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = false,
            ConsentGiven = false,
            ConsentGivenAt = null,
            ConsentVersion = _consentService.CurrentPolicyVersion,
            IsInviteAccepted = false,
            InviteToken = GenerateInviteToken(),
            InviteTokenExpiry = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = "Failed to create user.", errors = result.Errors });

        // Add to Identity role
        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return BadRequest(new { message = "Failed to assign role to user.", errors = roleResult.Errors });

        await SendInviteEmailAsync(request.Email, request.FullName, request.Role, user.InviteToken!);

        return Ok(new { message = $"Invitation sent to {request.Email}." });
    }

    // ── POST /api/auth/accept-invite ─────────────────────────────────────────
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConsentGiven)
            return BadRequest(new { message = "Consent must be accepted to activate the account." });

        // Find user by token
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == request.InviteToken);

        if (user == null)
            return BadRequest(new { message = "Invalid or expired invitation token." });

        if (user.IsInviteAccepted)
            return BadRequest(new { message = "This invitation has already been used." });

        if (user.InviteTokenExpiry == null || user.InviteTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "The invitation link has expired. Please request a new invite." });

        // Set password (self-signup users may already have one).
        if (await _userManager.HasPasswordAsync(user))
        {
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
                return BadRequest(new { message = "Password setup failed.", errors = removePasswordResult.Errors });
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
        if (!addPasswordResult.Succeeded)
            return BadRequest(new { message = "Password setup failed.", errors = addPasswordResult.Errors });

        // Mark invite accepted
        user.IsInviteAccepted = true;
        user.IsActive = true;
        user.ConsentGiven = true;
        user.ConsentGivenAt = DateTime.UtcNow;
        user.ConsentVersion = _consentService.CurrentPolicyVersion;
        user.UserStatus = "Approved";
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        await _consentService.RecordConsentAsync(
            user.Id,
            true,
            user.ConsentVersion,
            "invitation");

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            User = await MapToDtoAsync(user),
            RequiresConsent = false,
            RequiredConsentVersion = null
        });
    }

    // ── POST /api/auth/login ─────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        await _consentService.EnsurePolicyVersionAlignmentAsync(user);
        await _userManager.UpdateAsync(user);

        if (user.UserStatus == "Approved" && !user.IsInviteAccepted)
            return Unauthorized(new { message = "Your account is approved. Please check your email and accept the invitation to activate your account." });

        if (!user.IsActive || !user.IsInviteAccepted)
            return Unauthorized(new { message = "Your account is not yet approved. Please wait for admin approval." });

        if (user.UserStatus == "Pending")
            return Unauthorized(new { message = "Your account is not yet approved. Please wait for admin approval." });

        if (user.UserStatus == "Rejected")
            return Unauthorized(new { message = "Your account has been rejected. Please contact support." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Account is temporarily locked due to too many failed attempts." });
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.ConsentGiven)
        {
            var consentToken = GenerateJwtToken(user);
            return Ok(new AuthResponse
            {
                Token = consentToken,
                User = await MapToDtoAsync(user),
                RequiresConsent = true,
                RequiredConsentVersion = _consentService.CurrentPolicyVersion
            });
        }

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            User = await MapToDtoAsync(user),
            RequiresConsent = false,
            RequiredConsentVersion = null
        });
    }

    // ── POST /api/auth/signup/player ─────────────────────────────────────────
    [HttpPost("signup/player")]
    [AllowAnonymous]
    public async Task<IActionResult> SignupPlayer([FromBody] PlayerSignupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConsentGiven)
            return BadRequest(new { message = "Consent must be accepted before signup." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = "Player",
            SignupRole = "Player",
            UserStatus = "Pending",
            IsActive = false,
            ConsentGiven = true,
            ConsentGivenAt = DateTime.UtcNow,
            ConsentVersion = _consentService.CurrentPolicyVersion,
            IsInviteAccepted = false,
            InviteToken = null,
            InviteTokenExpiry = null,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Signup failed.", errors = result.Errors });

        await _userManager.AddToRoleAsync(user, "Player");
        await _consentService.RecordConsentAsync(user.Id, true, user.ConsentVersion, "signup");

        // Insert player profile into stf.players.
        try
        {
            var profileImageValue = request.ProfileImage;
            if (!string.IsNullOrWhiteSpace(profileImageValue) &&
                (profileImageValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || profileImageValue.Length > 500))
            {
                // Signup should send URL from /api/auth/upload-signup-image.
                // Ignore raw/base64 payloads to keep DB value valid.
                profileImageValue = null;
            }

            var player = new Player1
            {
                PlayerId = Guid.NewGuid().ToString(),
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Nationality = request.Nationality ?? string.Empty,
                PositionCode = string.Empty,
                PreferredFoot = string.Empty,
                HeightCm = request.HeightCm ?? 0,
                WeightKg = request.WeightKg ?? 0,
                AgentName = request.AgentName ?? string.Empty,
                AgentScoutId = null!,
                ContactInfo = string.IsNullOrWhiteSpace(request.ContactInfo) ? request.Pincode : request.ContactInfo,
                ProfileImageUrl = profileImageValue,
                playerEmail = request.Email,
                SportId = request.SportId,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                UserStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _footballContext.Players1.Add(player);
            await _footballContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert player record into stf.players for {Email}", request.Email);
            return StatusCode(500, new
            {
                message = "Account created but player profile could not be saved. Please contact support.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }

        // Non-blocking notifications for player self-signup flow.
        try
        {
            await NotifyAdminsOfNewSignupAsync(request.FullName, request.Email, "Player");
            await SendSignupSubmittedEmailAsync(request.Email, request.FullName, "Player");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send signup notification emails for {Email}", request.Email);
        }

        return Ok(new { message = "Signup submitted successfully. Please wait for admin approval." });
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SelfSignupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.Equals(request.SignupRole, "Player", StringComparison.OrdinalIgnoreCase))
        {
            return await SignupPlayer(new PlayerSignupRequest
            {
                Email = request.Email,
                FullName = request.FullName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                ConsentGiven = request.ConsentGiven,
                DateOfBirth = request.DateOfBirth,
                Nationality = request.Nationality,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                CurrentClub = request.CurrentClub,
                ContractStatus = request.ContractStatus,
                AgentName = request.AgentName,
                ContactInfo = request.ContactInfo,
                ProfileImage = request.ProfileImage,
                SportId = request.SportId,
                Pincode = request.Pincode,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
            });
        }

        if (string.Equals(request.SignupRole, "Scout", StringComparison.OrdinalIgnoreCase))
        {
            return await SignupCoach(new CoachSignupRequest
            {
                Email = request.Email,
                FullName = request.FullName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                ConsentGiven = request.ConsentGiven,
                RoleName = request.RoleName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Country = request.Country,
                SportId = request.SportId,
                ProfileImage = request.ProfileImage,
            });
        }

        return BadRequest(new { message = "signupRole must be Player or Scout." });
    }

    // ── POST /api/auth/signup/coach ──────────────────────────────────────────
    [HttpPost("signup/coach")]
    [AllowAnonymous]
    public async Task<IActionResult> SignupCoach([FromBody] CoachSignupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConsentGiven)
            return BadRequest(new { message = "Consent must be accepted before signup." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = "Scout",
            SignupRole = "Scout",
            UserStatus = "Pending",
            IsActive = false,
            ConsentGiven = true,
            ConsentGivenAt = DateTime.UtcNow,
            ConsentVersion = _consentService.CurrentPolicyVersion,
            IsInviteAccepted = false,
            InviteToken = GenerateInviteToken(),
            InviteTokenExpiry = DateTime.UtcNow.AddHours(48),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Signup failed.", errors = result.Errors });

        await _userManager.AddToRoleAsync(user, "Scout");
        await _consentService.RecordConsentAsync(user.Id, true, user.ConsentVersion, "signup");

        var scoutId = string.IsNullOrWhiteSpace(request.ScoutId)
            ? Guid.NewGuid().ToString()
            : request.ScoutId;

        try
        {
            var scout = new Scout
            {
                ScoutId = scoutId,
                ScoutName = request.FullName,
                RoleName = request.RoleName ?? "Scout",
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                PostalCode = request.Pincode,
                Country = request.Country,
                SportId = request.SportId,
                IsShowPlayer = false,
                UserStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _footballContext.Scouts.Add(scout);
            await _footballContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert scout record for {Email}", request.Email);
            return StatusCode(500, new
            {
                message = "Account created but scout profile could not be saved. Please contact support.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }

        // Non-blocking notifications for scout self-signup flow.
        try
        {
            await NotifyAdminsOfNewSignupAsync(request.FullName, request.Email, "Scout");
            await SendSignupSubmittedEmailAsync(request.Email, request.FullName, "Scout");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send signup notification emails for scout {Email}", request.Email);
        }

        return Ok(new { message = "Signup submitted successfully. Please wait for admin approval." });
    }

    // ── GET /api/auth/pending-users ──────────────────────────────────────────
    [HttpGet("pending-users")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetPendingUsers()
    {
        var pending = _userManager.Users
            .Where(u => u.UserStatus == "Pending" && u.SignupRole != null)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new PendingUserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                SignupRole = u.SignupRole ?? string.Empty,
                UserStatus = u.UserStatus,
                CreatedAt = u.CreatedAt
            })
            .ToList();

        return Ok(pending);
    }

    // ── POST /api/auth/approve-reject ─────────────────────────────────────────
    [HttpPost("approve-reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReject([FromBody] ApproveRejectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.UserStatus = request.Action; // "Approved" or "Rejected"
        if (request.Action == "Approved")
        {
            var isSelfSignupDirectActivationRole =
                string.Equals(user.SignupRole, "Player", StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.SignupRole, "Scout", StringComparison.OrdinalIgnoreCase);

            if (isSelfSignupDirectActivationRole)
            {
                // Self-signup Player/Scout users already set password during signup; activate directly.
                user.IsActive = true;
                user.IsInviteAccepted = true;
                user.InviteToken = null;
                user.InviteTokenExpiry = null;
                user.EmailConfirmed = true;
            }
            else
            {
                user.IsActive = false;
                user.IsInviteAccepted = false;
                user.InviteToken = GenerateInviteToken();
                user.InviteTokenExpiry = DateTime.UtcNow.AddHours(48);
                user.EmailConfirmed = false;
            }
        }
        else
        {
            user.IsActive = false;
        }

        await _userManager.UpdateAsync(user);

        // Update player/scout record status
        if (user.SignupRole == "Player")
        {
            var player = await _footballContext.Players1
                .FirstOrDefaultAsync(p => p.playerEmail == user.Email);
            if (player != null)
            {
                player.UserStatus = request.Action;
                await _footballContext.SaveChangesAsync();
            }
        }
        else if (user.SignupRole == "Scout")
        {
            var scout = await _footballContext.Scouts
                .FirstOrDefaultAsync(s => s.Email == user.Email);
            if (scout != null)
            {
                scout.UserStatus = request.Action;
                await _footballContext.SaveChangesAsync();
            }
        }

        // Send email notification
        try
        {
            if (request.Action == "Approved")
            {
                if (string.Equals(user.SignupRole, "Scout", StringComparison.OrdinalIgnoreCase))
                {
                    await SendScoutApprovedEmailAsync(user.Email!, user.FullName);
                }
                else if (string.Equals(user.SignupRole, "Player", StringComparison.OrdinalIgnoreCase))
                {
                    await SendApprovalEmailAsync(user.Email!, user.FullName, request.Action);
                }
                else
                {
                    await SendInviteEmailAsync(user.Email!, user.FullName, user.Role, user.InviteToken!);
                }
            }
            else
            {
                await SendApprovalEmailAsync(user.Email!, user.FullName, request.Action);
            }
        }
        catch { /* non-blocking */ }

        return Ok(new { message = $"User has been {request.Action.ToLower()}." });
    }

    // ── GET /api/auth/me ─────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "User not found or inactive." });

        return Ok(await MapToDtoAsync(user));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GenerateJwtToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryHours = _config.GetValue<int>("Jwt:ExpiryHours", 8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName),
            new Claim("role", user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateInviteToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private async System.Threading.Tasks.Task SendInviteEmailAsync(string email, string fullName, string role, string inviteToken)
    {
        var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:5173";
        var inviteLink = BuildAcceptInviteLink(frontendUrl, inviteToken);
        var emailHtml = BuildInviteEmailHtml(fullName, role, inviteLink);

        try
        {
            await _emailService.SendEmailAsync(
                email,
                fullName,
                "You've been invited to Football Dashboard",
                emailHtml);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invitation email could not be sent to '{email}'. {ex.Message}", ex);
        }
    }

    private static string BuildAcceptInviteLink(string frontendUrl, string inviteToken)
    {
        var baseUrl = frontendUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(inviteToken);

        // Frontend currently uses HashRouter, so invite links must include /#/.
        if (baseUrl.Contains("/#", StringComparison.Ordinal))
            return $"{baseUrl}/accept-invite?token={encodedToken}";

        return $"{baseUrl}/#/accept-invite?token={encodedToken}";
    }

    private async System.Threading.Tasks.Task NotifyAdminsOfNewSignupAsync(string fullName, string email, string role)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var html = BuildSignupNotificationHtml(fullName, email, role);
        foreach (var admin in admins)
        {
            if (!string.IsNullOrWhiteSpace(admin.Email))
                await _emailService.SendEmailAsync(admin.Email, admin.FullName,
                    $"New {role} Signup Pending Approval", html);
        }
    }

    private async System.Threading.Tasks.Task SendApprovalEmailAsync(string email, string fullName, string action)
    {
        var subject = action == "Approved"
            ? "Your account has been approved!"
            : "Your account application status";
        var html = BuildApprovalEmailHtml(fullName, action);
        await _emailService.SendEmailAsync(email, fullName, subject, html);
    }

    private async System.Threading.Tasks.Task SendSignupSubmittedEmailAsync(string email, string fullName, string role)
    {
        var subject = "Signup request received";
        var html = BuildSignupSubmittedEmailHtml(fullName, role);
        await _emailService.SendEmailAsync(email, fullName, subject, html);
    }

    private async System.Threading.Tasks.Task SendScoutApprovedEmailAsync(string email, string fullName)
    {
        var subject = "Your scout account has been approved";
        var html = BuildScoutApprovedEmailHtml(fullName);
        await _emailService.SendEmailAsync(email, fullName, subject, html);
    }

    private async Task<UserInfoDto> MapToDtoAsync(ApplicationUser user)
    {
        string? scoutId = null;
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            scoutId = await _footballContext.Scouts
                .AsNoTracking()
                .Where(s => (s.Email ?? string.Empty).ToLower() == user.Email.ToLower())
                .Select(s => s.ScoutId)
                .FirstOrDefaultAsync();
        }

        // Also try to resolve a player ID when the user is a Player
        string? playerId = null;
        if (string.Equals(user.Role, "Player", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(user.Email))
        {
            playerId = await _footballContext.Players1
                .AsNoTracking()
                .Where(p => (p.playerEmail ?? string.Empty).ToLower() == user.Email.ToLower())
                .Select(p => p.PlayerId)
                .FirstOrDefaultAsync();
        }
        
        var dto = new UserInfoDto
        {
            Id = user.Id,
            ScoutId = scoutId,
            Email = user.Email!,
            FullName = user.FullName,
            Role = user.Role,
            UserStatus = user.UserStatus,
            ConsentGiven = user.ConsentGiven,
            ConsentGivenAt = user.ConsentGivenAt,
            ConsentVersion = user.ConsentVersion ?? "v1.0",
            IsActive = user.IsActive
        };
        
        // Populate LoginUser to help frontend open the correct profile page/modal
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            dto.LoginUser = new LoginUserDto { Id = playerId, Type = "Player" };
        }
        else if (!string.IsNullOrWhiteSpace(scoutId))
        {
            dto.LoginUser = new LoginUserDto { Id = scoutId, Type = "Scout" };
        }
        
        return dto;
    }

    private static string BuildInviteEmailHtml(string fullName, string role, string inviteLink) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f5; margin: 0; padding: 20px;'>
  <div style='max-width: 600px; margin: auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
    <div style='background: #1d4ed8; padding: 24px 32px;'>
      <h1 style='color: white; margin: 0; font-size: 22px;'>⚽ Football Dashboard</h1>
    </div>
    <div style='padding: 32px;'>
      <h2 style='margin-top: 0; color: #111827;'>You're Invited!</h2>
      <p style='color: #374151; font-size: 15px;'>Hi <strong>{fullName}</strong>,</p>
      <p style='color: #374151; font-size: 15px;'>
        You have been invited to join the <strong>Football Scout Dashboard</strong> as a
        <strong style='color: #1d4ed8;'>{role}</strong>.
      </p>
      <p style='color: #374151; font-size: 15px;'>
        Click the button below to set your password and activate your account:
      </p>
      <div style='text-align: center; margin: 32px 0;'>
        <a href='{inviteLink}'
           style='background: #1d4ed8; color: white; text-decoration: none; padding: 14px 32px;
                  border-radius: 6px; font-size: 16px; font-weight: bold; display: inline-block;'>
          Accept Invitation
        </a>
      </div>
      <p style='color: #6b7280; font-size: 13px;'>
        Or copy this link into your browser:<br/>
        <a href='{inviteLink}' style='color: #1d4ed8; word-break: break-all;'>{inviteLink}</a>
      </p>
      <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;'/>
      <p style='color: #9ca3af; font-size: 12px; margin: 0;'>
        ⚠️ This invitation link expires in <strong>48 hours</strong>. If you did not expect this email, you can safely ignore it.
      </p>
    </div>
  </div>
</body>
</html>";

    private static string BuildSignupNotificationHtml(string fullName, string email, string role) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background-color:#f4f4f5; margin:0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.1);'>
    <div style='background:#1d4ed8; padding:24px 32px;'>
      <h1 style='color:white; margin:0; font-size:22px;'>⚽ Football Dashboard — New Signup</h1>
    </div>
    <div style='padding:32px;'>
      <p style='color:#374151; font-size:15px;'>A new <strong>{role}</strong> has registered and is pending your approval:</p>
      <table style='width:100%; border-collapse:collapse; font-size:14px;'>
        <tr><td style='padding:6px 0; color:#6b7280;'>Name</td><td style='padding:6px 0; font-weight:bold;'>{fullName}</td></tr>
        <tr><td style='padding:6px 0; color:#6b7280;'>Email</td><td style='padding:6px 0;'>{email}</td></tr>
        <tr><td style='padding:6px 0; color:#6b7280;'>Role</td><td style='padding:6px 0;'>{role}</td></tr>
      </table>
      <p style='color:#374151; font-size:14px; margin-top:20px;'>Log in to the admin dashboard to approve or reject this account.</p>
    </div>
  </div>
</body>
</html>";

    private static string BuildApprovalEmailHtml(string fullName, string action)
    {
        var isApproved = action == "Approved";
        var color = isApproved ? "#16a34a" : "#dc2626";
        var message = isApproved
            ? "Your account has been <strong style='color:#16a34a;'>approved</strong>! You can now log in to the Football Dashboard."
            : "Unfortunately, your account application has been <strong style='color:#dc2626;'>rejected</strong>. Please contact support for more information.";

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif; background-color:#f4f4f5; margin:0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.1);'>
    <div style='background:{color}; padding:24px 32px;'>
      <h1 style='color:white; margin:0; font-size:22px;'>⚽ Football Dashboard</h1>
    </div>
    <div style='padding:32px;'>
      <h2 style='margin-top:0; color:#111827;'>Account Status Update</h2>
      <p style='color:#374151; font-size:15px;'>Hi <strong>{fullName}</strong>,</p>
      <p style='color:#374151; font-size:15px;'>{message}</p>
    </div>
  </div>
</body>
</html>";
    }

        private static string BuildSignupSubmittedEmailHtml(string fullName, string role) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif; background-color:#f4f4f5; margin:0; padding:20px;'>
    <div style='max-width:600px; margin:auto; background:white; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.1);'>
        <div style='background:#1d4ed8; padding:24px 32px;'>
            <h1 style='color:white; margin:0; font-size:22px;'>⚽ Football Dashboard</h1>
        </div>
        <div style='padding:32px;'>
            <h2 style='margin-top:0; color:#111827;'>Signup Received</h2>
            <p style='color:#374151; font-size:15px;'>Hi <strong>{fullName}</strong>,</p>
            <p style='color:#374151; font-size:15px;'>
                Your <strong>{role}</strong> signup request has been submitted successfully.
            </p>
            <p style='color:#374151; font-size:15px;'>
                Our admin team will review your request. You will receive another email once your account is approved or rejected.
            </p>
        </div>
    </div>
</body>
</html>";

        private static string BuildScoutApprovedEmailHtml(string fullName) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif; background-color:#f4f4f5; margin:0; padding:20px;'>
    <div style='max-width:600px; margin:auto; background:white; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.1);'>
        <div style='background:#16a34a; padding:24px 32px;'>
            <h1 style='color:white; margin:0; font-size:22px;'>Football Dashboard</h1>
        </div>
        <div style='padding:32px;'>
            <h2 style='margin-top:0; color:#111827;'>Scout Account Approved</h2>
            <p style='color:#374151; font-size:15px;'><strong>Scout Name:</strong> {fullName}</p>
            <p style='color:#374151; font-size:15px;'>Your scout account has been approved. You can now log in.</p>
        </div>
    </div>
</body>
</html>";
}
