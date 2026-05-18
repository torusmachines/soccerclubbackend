using FootballDashboardAPI.Models;
using FootballDashboardAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;

namespace FootballDashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts([FromQuery] ContractQueryParameters filters)
    {
        var contracts = await _contractService.GetContractsAsync(filters);
        return Ok(contracts);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetContractAlerts(
        [FromQuery] string? contractType,
        [FromQuery] int daysAhead = 60,
        [FromQuery] int? limit = null)
    {
        if (daysAhead <= 0)
            daysAhead = 60;

        if (limit.HasValue && limit.Value <= 0)
            limit = null;

        if (limit.HasValue && limit.Value > 1000)
            limit = 1000;

        var alerts = await _contractService.GetContractAlertsAsync(contractType, daysAhead, limit);
        return Ok(alerts);
    }

    [HttpGet("by-club/{clubId}")]
    public async Task<IActionResult> GetContractsByClub(string clubId)
    {
        if (!Guid.TryParse(clubId, out var clubGuid))
            return BadRequest(new { error = "clubId must be a valid GUID" });

        var contracts = await _contractService.GetContractsAsync(new ContractQueryParameters
        {
            PartyType = "Club",
            PartyId = clubGuid
        });

        return Ok(contracts);
    }

    [HttpGet("by-player/{playerId}")]
    public async Task<IActionResult> GetContractsByPlayer(string playerId)
    {
        if (!Guid.TryParse(playerId, out var playerGuid))
            return BadRequest(new { error = "playerId must be a valid GUID" });

        var contracts = await _contractService.GetContractsAsync(new ContractQueryParameters
        {
            PartyType = "Player",
            PartyId = playerGuid
        });

        return Ok(contracts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var contract = await _contractService.GetContractByIdAsync(id);
        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractDto dto)
    {
        if (dto == null)
            return BadRequest(new { error = "Request body is required and must be valid JSON" });

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(new { error = "Validation failed", details = errors });
        }

        var (contract, validationErrors) = dto.ToContract();
        if (validationErrors != null && validationErrors.Any())
            return BadRequest(new { error = "Validation failed", details = validationErrors });

        if (contract == null)
            return BadRequest(new { error = "Failed to build contract from request" });

        var created = await _contractService.CreateContractAsync(contract);
        return CreatedAtAction(nameof(GetContract), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] CreateContractDto dto)
    {
        if (dto == null)
            return BadRequest(new { error = "Request body is required and must be valid JSON" });

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(new { error = "Validation failed", details = errors });
        }

        var (contract, validationErrors) = dto.ToContract();
        if (validationErrors != null && validationErrors.Any())
            return BadRequest(new { error = "Validation failed", details = validationErrors });

        if (contract == null)
            return BadRequest(new { error = "Failed to build contract from request" });

        var updated = await _contractService.UpdateContractAsync(id, contract);
        if (updated == null)
            return NotFound(new { error = "Contract not found" });

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContract(Guid id)
    {
        var deleted = await _contractService.DeleteContractAsync(id);
        if (!deleted)
            return NotFound(new { error = "Contract not found" });

        return NoContent();
    }

    [HttpPost("{id}/upload-document")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] List<IFormFile> files)
    {
        var contractResponse = await _contractService.GetContractByIdAsync(id);
        if (contractResponse == null)
            return NotFound(new { error = "Contract not found" });

        if (files == null || !files.Any())
            return BadRequest(new { error = "No files uploaded" });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var filePaths = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;

            var fileName = $"{id}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            filePaths.Add($"/documents/{fileName}");
        }

        if (!filePaths.Any())
            return BadRequest(new { error = "No valid files uploaded" });

        var documentPath = contractResponse.DocumentPath;
        if (!string.IsNullOrEmpty(documentPath))
        {
            var existingPaths = documentPath.Split(new[] { ",,," }, StringSplitOptions.None).ToList();
            existingPaths.AddRange(filePaths);
            documentPath = string.Join(",,,", existingPaths);
        }
        else
        {
            documentPath = string.Join(",,,", filePaths);
        }

        var contract = new Contract
        {
            Id = contractResponse.Id,
            Party1Id = contractResponse.Party1Id,
            Party1Type = contractResponse.Party1Type,
            Party1Name = contractResponse.Party1Name,
            Party2Id = contractResponse.Party2Id,
            Party2Type = contractResponse.Party2Type,
            Party2Name = contractResponse.Party2Name,
            ContractType = contractResponse.ContractType,
            StartDate = contractResponse.StartDate,
            EndDate = contractResponse.EndDate,
            ExpiryDate = contractResponse.ExpiryDate != null ? contractResponse.ExpiryDate : null,
            ContractDetails = contractResponse.ContractDetails,
            DocumentPath = documentPath,
            CreatedAt = contractResponse.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };
        var updated = await _contractService.UpdateContractAsync(id, contract);
        if (updated == null)
            return NotFound(new { error = "Contract not found" });

        return Ok(new { documentPath = documentPath });
    }

    [HttpDelete("{id}/documents")]
    public async Task<IActionResult> DeleteDocument(Guid id, [FromQuery] string documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
            return BadRequest(new { error = "Document path is required" });

        var contractResponse = await _contractService.GetContractByIdAsync(id);
        if (contractResponse == null)
            return NotFound(new { error = "Contract not found" });

        var existingPaths = string.IsNullOrEmpty(contractResponse.DocumentPath)
            ? new List<string>()
            : contractResponse.DocumentPath.Split(new[] { ",,," }, StringSplitOptions.None).ToList();

        if (!existingPaths.Remove(documentPath))
            return NotFound(new { error = "Document not found" });

        var relativePath = documentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch
            {
                // ignore delete failures, path will be removed from the contract record
            }
        }

        var updatedDocumentPath = existingPaths.Any() ? string.Join(",,,", existingPaths) : null;
        var contract = new Contract
        {
            Id = contractResponse.Id,
            Party1Id = contractResponse.Party1Id,
            Party1Type = contractResponse.Party1Type,
            Party1Name = contractResponse.Party1Name,
            Party2Id = contractResponse.Party2Id,
            Party2Type = contractResponse.Party2Type,
            Party2Name = contractResponse.Party2Name,
            ContractType = contractResponse.ContractType,
            StartDate = contractResponse.StartDate,
            EndDate = contractResponse.EndDate,
            ExpiryDate = contractResponse.ExpiryDate != null ? contractResponse.ExpiryDate : null,
            ContractDetails = contractResponse.ContractDetails,
            DocumentPath = updatedDocumentPath,
            CreatedAt = contractResponse.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };
        var updated = await _contractService.UpdateContractAsync(id, contract);
        if (updated == null)
            return NotFound(new { error = "Contract not found" });

        return Ok(new { documentPath = updatedDocumentPath });
    }
}
