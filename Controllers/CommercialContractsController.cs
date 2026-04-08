using FootballDashboardAPI.Data;
using FootballDashboardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommercialContractsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommercialContractsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts()
    {
        var contracts = await _context.CommercialContracts
            .Include(c => c.Sponsor)
            .ToListAsync();
        return Ok(contracts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var contract = await _context.CommercialContracts
            .Include(c => c.Sponsor)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract == null) return NotFound();
        return Ok(contract);
    }

    [HttpGet("by-club/{clubId}")]
    public async Task<IActionResult> GetContractsByClub(string clubId)
    {
        var contracts = await _context.CommercialContracts
            .Include(c => c.Sponsor)
            .Where(c => c.EntityType == "club" && c.ClubId == clubId)
            .ToListAsync();
        return Ok(contracts);
    }

    [HttpGet("by-player/{playerId}")]
    public async Task<IActionResult> GetContractsByPlayer(string playerId)
    {
        var contracts = await _context.CommercialContracts
            .Include(c => c.Sponsor)
            .Where(c => c.EntityType == "player" && c.PlayerId == playerId)
            .ToListAsync();
        return Ok(contracts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateCommercialContractDto dto)
    {
        // Validate that DTO was deserialized
        if (dto == null)
            return BadRequest(new { error = "Request body is required and must be valid JSON" });

        // Validate DTO properties
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(new { error = "Validation failed", details = errors });
        }

        // Convert DTO to model with validation
        var (contract, validationErrors) = dto.ToCommercialContract();

        if (validationErrors != null && validationErrors.Any())
            return BadRequest(new { error = "Validation failed", details = validationErrors });

        if (contract == null)
            return BadRequest(new { error = "Failed to create contract from provided data" });

        // Add to database and save
        _context.CommercialContracts.Add(contract);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetContract), new { id = contract.Id }, contract);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] CreateCommercialContractDto dto)
    {
        if (dto == null)
            return BadRequest(new { error = "Request body is required and must be valid JSON" });

        var contract = await _context.CommercialContracts.FindAsync(id);
        if (contract == null) 
            return NotFound(new { error = "Contract not found" });

        // Validate DTO properties
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(new { error = "Validation failed", details = errors });
        }

        // Convert DTO to model with validation
        var (updatedContractData, validationErrors) = dto.ToCommercialContract();

        if (validationErrors != null && validationErrors.Any())
            return BadRequest(new { error = "Validation failed", details = validationErrors });

        if (updatedContractData == null)
            return BadRequest(new { error = "Failed to update contract from provided data" });

        // Update existing contract properties
        contract.SponsorId = updatedContractData.SponsorId;
        contract.EntityType = updatedContractData.EntityType;
        contract.ClubId = updatedContractData.ClubId;
        contract.PlayerId = updatedContractData.PlayerId;
        contract.ContractStartDate = updatedContractData.ContractStartDate;
        contract.ContractEndDate = updatedContractData.ContractEndDate;
        contract.ExpiryDate = updatedContractData.ExpiryDate;
        contract.ContractDetails = updatedContractData.ContractDetails;
        contract.DocumentPath = updatedContractData.DocumentPath;
        contract.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(contract);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContract(Guid id)
    {
        var contract = await _context.CommercialContracts.FindAsync(id);
        if (contract == null) return NotFound();

        _context.CommercialContracts.Remove(contract);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/upload-document")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] List<IFormFile> files)
    {
        Console.WriteLine($"UploadDocument called for contract {id} with {files?.Count ?? 0} files");
        
        var contract = await _context.CommercialContracts.FindAsync(id);
        if (contract == null) return NotFound();

        if (files == null || !files.Any()) {
            Console.WriteLine("No files uploaded");
            return BadRequest("No files uploaded");
        }

        Console.WriteLine($"Processing {files.Count} files");

        // Save files to wwwroot/documents or similar
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var filePaths = new List<string>();

        foreach (var file in files)
        {
            Console.WriteLine($"Processing file: {file.FileName}, size: {file.Length}");
            
            if (file.Length == 0) continue;

            var fileName = $"{id}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            filePaths.Add($"/documents/{fileName}");
        }

        // Store paths as triple-comma-separated string (append to existing)
        var existingPaths = string.IsNullOrEmpty(contract.DocumentPath) 
            ? new List<string>() 
            : contract.DocumentPath.Split(new[] { ",,," }, StringSplitOptions.None).ToList();
        existingPaths.AddRange(filePaths);
        contract.DocumentPath = string.Join(",,,", existingPaths);
        contract.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        Console.WriteLine($"Saved document paths: {contract.DocumentPath}");

        return Ok(new { documentPath = contract.DocumentPath });
    }

    [HttpDelete("{id}/documents")]
    public async Task<IActionResult> DeleteDocument(Guid id, [FromQuery] string documentPath)
    {
        if (string.IsNullOrEmpty(documentPath))
            return BadRequest(new { error = "Document path is required" });

        var contract = await _context.CommercialContracts.FindAsync(id);
        if (contract == null) return NotFound(new { error = "Contract not found" });

        var existingPaths = string.IsNullOrEmpty(contract.DocumentPath)
            ? new List<string>()
            : contract.DocumentPath.Split(new[] { ",,," }, StringSplitOptions.None).ToList();

        if (!existingPaths.Remove(documentPath))
            return NotFound(new { error = "Document not found" });

        var relativePath = documentPath.TrimStart('/');
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file {fullPath}: {ex.Message}");
            }
        }

        contract.DocumentPath = existingPaths.Any() ? string.Join(",,,", existingPaths) : null;
        contract.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { documentPath = contract.DocumentPath });
    }
}