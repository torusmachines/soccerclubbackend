using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SponsorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SponsorsController(AppDbContext context)
    {
        _context = context;
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
}