using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ContactRolesController : ControllerBase
{
    private readonly IContactRoleService _service;

    public ContactRolesController(IContactRoleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContactRole>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactRole>> Get(string id)
    {
        var item = await _service.GetByIdAsync(id);

        if (item == null)
            return NotFound(new { message = "ContactRole not found" });

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContactRole>> Create(CreateContactRole dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var item = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(Get), new { id = item.RoleId }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContactRole>> Update(string id, UpdateContactRole dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);

            if (item == null)
                return NotFound(new { message = "ContactRole not found" });

            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Contact role not found" });

            return Ok(new { message = "Contact role deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}