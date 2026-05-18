using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SponsorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailNotificationService _emailService;

    public SponsorsController(AppDbContext context, IEmailNotificationService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSponsors()
    {
        var sponsors = await _context.Sponsors.ToListAsync();
        return Ok(sponsors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSponsor(Guid id)
    {
        var sponsor = await _context.Sponsors.FindAsync(id);
        if (sponsor == null) return NotFound();
        return Ok(sponsor);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSponsor([FromBody] Sponsor sponsor)
    {
        sponsor.Id = Guid.NewGuid();
        sponsor.CreatedAt = DateTime.UtcNow;
        sponsor.UpdatedAt = DateTime.UtcNow;
        _context.Sponsors.Add(sponsor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSponsor), new { id = sponsor.Id }, sponsor);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSponsor(Guid id, [FromBody] Sponsor updatedSponsor)
    {
        var sponsor = await _context.Sponsors.FindAsync(id);
        if (sponsor == null) return NotFound();

        sponsor.CompanyName = updatedSponsor.CompanyName;
        sponsor.ContactName = updatedSponsor.ContactName;
        sponsor.ContactEmail = updatedSponsor.ContactEmail;
        sponsor.ContactPhone = updatedSponsor.ContactPhone;
        sponsor.Address = updatedSponsor.Address;
        sponsor.Notes = updatedSponsor.Notes;
        sponsor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(sponsor);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSponsor(Guid id)
    {
        var sponsor = await _context.Sponsors.FindAsync(id);
        if (sponsor == null) return NotFound();

        _context.Sponsors.Remove(sponsor);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetSponsorComments(Guid id)
    {
        var sponsorExists = await _context.Sponsors.AnyAsync(s => s.Id == id);
        if (!sponsorExists) return NotFound();

        var comments = await _context.SponsorComments
            .Where(c => c.SponsorId == id && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost("{id}/comments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSponsorComment(Guid id, [FromBody] CreateSponsorCommentRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { error = "Comment is required" });

        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.Id == id);
        if (sponsor == null) return NotFound();

        if (string.IsNullOrWhiteSpace(sponsor.ContactEmail))
            return BadRequest(new { error = "Sponsor contact email is not configured" });

        var comment = new SponsorComment
        {
            CommentId = Guid.NewGuid(),
            SponsorId = id,
            Comment = request.Comment.Trim(),
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CreatedByName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email),
            CreatedByRole = User.FindFirstValue(ClaimTypes.Role),
            IsAdminComment = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        _context.SponsorComments.Add(comment);
        await _context.SaveChangesAsync();

        var commenterName = comment.CreatedByName ?? "Admin";
        var subject = $"New comment from admin - {sponsor.CompanyName}";
        var html = BuildSponsorCommentEmailHtml(
            sponsorName: sponsor.CompanyName,
            sponsorContactName: sponsor.ContactName,
            commentText: comment.Comment,
            commenterName: commenterName,
            createdAtUtc: comment.CreatedAt);

        await _emailService.SendEmailAsync(
            sponsor.ContactEmail,
            sponsor.ContactName ?? sponsor.CompanyName,
            subject,
            html);

        return Ok(comment);
    }

    private static string BuildSponsorCommentEmailHtml(
        string sponsorName,
        string? sponsorContactName,
        string commentText,
        string commenterName,
        DateTime createdAtUtc)
    {
        var displayName = string.IsNullOrWhiteSpace(sponsorContactName) ? "Sponsor Contact" : sponsorContactName;
        var safeComment = System.Net.WebUtility.HtmlEncode(commentText);

        return $@"
            <div style='font-family:Arial,sans-serif;line-height:1.6;color:#1f2937'>
                <p>Dear {displayName},</p>
                <p>A new admin comment has been added for your sponsor profile <strong>{sponsorName}</strong>.</p>
                <div style='border-left:4px solid #2563eb;padding:10px 12px;background:#f8fafc;margin:12px 0'>
                    <p style='margin:0'><strong>Comment:</strong> {safeComment}</p>
                </div>
                <p style='margin:0'><strong>Added by:</strong> {commenterName}</p>
                <p style='margin:0'><strong>Added at (UTC):</strong> {createdAtUtc:yyyy-MM-dd HH:mm}</p>
                <p style='margin-top:16px'>Regards,<br/>Football Dashboard Team</p>
            </div>";
    }

    public class CreateSponsorCommentRequest
    {
        public string? Comment { get; set; }
    }
}