using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballDashboardAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;

    public DocumentsController(IDocumentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("player/{playerId}")]
    public async Task<IActionResult> GetByPlayer(string playerId)
    {
        var documents = await _service.GetByPlayerIdAsync(playerId);

        if (User.IsInRole("Player"))
        {
            documents = documents.Where(doc => doc.IsVisibleToPlayer).ToList();
        }

        return Ok(documents);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id)
    {
        var doc = await _service.GetByIdAsync(id);

        if (doc == null)
            return NotFound();

        if (User.IsInRole("Player") && !(doc.IsVisibleToPlayer))
            return Forbid();

        return Ok(new
        {
            documentId = doc.DocumentId,
            documentName = doc.DocumentName,
            documentType = doc.DocumentType,
            fileData = doc.FileData != null ? Convert.ToBase64String(doc.FileData) : null
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocument dto)
    {
        var doc = await _service.CreateAsync(dto);
        return Ok(doc);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateDocument dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (!result)
            return NotFound();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}