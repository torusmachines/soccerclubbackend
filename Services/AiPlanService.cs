using System.Text.Json;
using System.Text;
using FootballDashboardAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FootballDashboardAPI.Services;

public class AiPlanService : IAiPlanService
{
    private readonly FootballContext _context;
    private readonly IAiService _aiService;
    private readonly ILogger<AiPlanService> _logger;
    private readonly IWebHostEnvironment _environment;

    public AiPlanService(FootballContext context, IAiService aiService, ILogger<AiPlanService> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
        _environment = environment;
    }

    public async Task<AiPlanResponse> GenerateAiPlanAsync(string playerId, AiPlanGenerateRequest? request = null)
    {
        _logger.LogInformation("Generating AI plan for player {PlayerId}", playerId);

        var normalizedRequest = NormalizeRequest(request);

        // Get player data
        var player = await _context.Players1
            .FirstOrDefaultAsync(p => p.PlayerId == playerId);

        if (player == null)
        {
            throw new ArgumentException($"Player with ID {playerId} not found");
        }

        // Aggregate data
        var notes = await GetPlayerNotesAsync(playerId);
        var reviews = await GetPlayerReviewsAsync(playerId);
        var ratings = await GetPlayerRatingsAsync(playerId);
        var previousPlans = await GetPreviousPlansAsync(playerId);
        var version = await GetNextVersionAsync(playerId);

        // Persist input details first as requested, then fill generated content.
        var planId = Guid.NewGuid().ToString();
        var aiPlan = new PlayerAiPlan
        {
            PlanId = planId,
            PlayerId = playerId,
            PlanJson = "{}",
            Version = version,
            SkillType = normalizedRequest.SkillType,
            CurrentLevel = normalizedRequest.CurrentLevel,
            TargetLevel = normalizedRequest.TargetLevel,
            DurationWeeks = normalizedRequest.DurationWeeks,
            TrainingDaysPerWeek = normalizedRequest.TrainingDaysPerWeek,
            SessionDurationMinutes = normalizedRequest.SessionDurationMinutes,
            HasInjury = normalizedRequest.HasInjury,
            InjuryDetails = normalizedRequest.HasInjury ? normalizedRequest.InjuryDetails?.Trim() : null,
            CreatedAt = DateTime.UtcNow
        };
        _context.PlayerAiPlans.Add(aiPlan);
        await _context.SaveChangesAsync();

        // Preprocess data
        var processedData = PreprocessPlayerData(player, notes, reviews, ratings, previousPlans);

        // Build prompt
        var prompt = BuildAiPrompt(processedData, normalizedRequest);

        // Call AI service
        var aiResponse = await _aiService.GeneratePlayerDevelopmentPlanAsync(prompt);

        // Validate and parse JSON
        var planContent = ParseAiResponse(aiResponse);

        var pdfPath = await CreateAiPlanPdfAsync(player, planId, version, planContent, normalizedRequest);

        aiPlan.PlanJson = JsonSerializer.Serialize(planContent);
        aiPlan.RawText = aiResponse;
        aiPlan.PdfPath = pdfPath;
        await _context.SaveChangesAsync();

        return MapToResponse(aiPlan, planContent);
    }

    public async Task<AiPlanResponse?> GetLatestAiPlanAsync(string playerId)
    {
        var latestPlan = await _context.PlayerAiPlans
            .Where(p => p.PlayerId == playerId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();

        if (latestPlan == null)
        {
            _logger.LogInformation("No AI plan found for player {PlayerId}. Generating a new plan.", playerId);
            return await GenerateAiPlanAsync(playerId);
        }

        var planContent = JsonSerializer.Deserialize<AiPlanContent>(latestPlan.PlanJson);
        return MapToResponse(latestPlan, planContent!);
    }

    public async Task<AiPlanHistoryResponse> GetAiPlanHistoryAsync(string playerId)
    {
        var plans = await _context.PlayerAiPlans
            .Where(p => p.PlayerId == playerId)
            .OrderByDescending(p => p.Version)
            .ToListAsync();

        if (!plans.Any())
        {
            _logger.LogInformation("No AI history found for player {PlayerId}. Generating first plan.", playerId);
            var generatedPlan = await GenerateAiPlanAsync(playerId);
            return new AiPlanHistoryResponse { Plans = new List<AiPlanResponse> { generatedPlan } };
        }

        var planResponses = new List<AiPlanResponse>();

        foreach (var plan in plans)
        {
            var planContent = JsonSerializer.Deserialize<AiPlanContent>(plan.PlanJson);
            planResponses.Add(MapToResponse(plan, planContent!));
        }

        return new AiPlanHistoryResponse { Plans = planResponses };
    }

    private async Task<List<Note>> GetPlayerNotesAsync(string playerId)
    {
        return await _context.Notes
            .Where(n => n.PlayerId == playerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20) // Limit to last 20 notes
            .ToListAsync();
    }

    private async Task<List<Review>> GetPlayerReviewsAsync(string playerId)
    {
        return await _context.Reviews
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10) // Limit to last 10 reviews
            .ToListAsync();
    }

    private async Task<List<ReviewRating>> GetPlayerRatingsAsync(string playerId)
    {
        return await _context.ReviewRatings
            .Where(rr => _context.Reviews.Any(r => r.ReviewId == rr.ReviewId && r.PlayerId == playerId))
            .Include(rr => rr.Review)
            .OrderByDescending(rr => rr.Review!.CreatedAt)
            .Take(10) // Limit to last 10 rating sets
            .ToListAsync();
    }

    private async Task<List<PlayerAiPlan>> GetPreviousPlansAsync(string playerId)
    {
        return await _context.PlayerAiPlans
            .Where(p => p.PlayerId == playerId)
            .OrderByDescending(p => p.Version)
            .Take(3) // Get last 3 plans
            .ToListAsync();
    }

    private async Task<int> GetNextVersionAsync(string playerId)
    {
        var maxVersion = await _context.PlayerAiPlans
            .Where(p => p.PlayerId == playerId)
            .MaxAsync(p => (int?)p.Version);

        return (maxVersion ?? 0) + 1;
    }

    private ProcessedPlayerData PreprocessPlayerData(
        Player1 player,
        List<Note> notes,
        List<Review> reviews,
        List<ReviewRating> ratings,
        List<PlayerAiPlan> previousPlans)
    {
        // Calculate age
        var age = player.DateOfBirth != default
            ? DateTime.Now.Year - player.DateOfBirth.Year
            : 25; // Default age if not available

        // Group notes by category
        var notesByCategory = notes
            .GroupBy(n => n.Category)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Description).ToList());

        // Calculate rating trends
        var ratingTrends = CalculateRatingTrends(ratings);

        // Detect injury risks
        var injuryRisks = DetectInjuryRisks(notes);

        // Extract previous plan summaries
        var previousPlanSummaries = previousPlans
            .Select(p => JsonSerializer.Deserialize<AiPlanContent>(p.PlanJson)?.Summary ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        return new ProcessedPlayerData
        {
            Player = player,
            Age = age,
            NotesByCategory = notesByCategory,
            Reviews = reviews.Select(r => r.Notes ?? "").ToList(),
            RatingTrends = ratingTrends,
            InjuryRisks = injuryRisks,
            PreviousPlanSummaries = previousPlanSummaries
        };
    }

    private Dictionary<string, object> CalculateRatingTrends(List<ReviewRating> ratings)
    {
        if (!ratings.Any())
        {
            return new Dictionary<string, object>();
        }

        var trends = new Dictionary<string, object>();

        // Calculate average ratings
        trends["passing"] = ratings.Average(r => (double)r.Passing);
        trends["shooting"] = ratings.Average(r => (double)r.Shooting);
        trends["dribbling"] = ratings.Average(r => (double)r.Dribbling);
        trends["tacticalAwareness"] = ratings.Average(r => (double)r.TacticalAwareness);
        trends["defensiveContribution"] = ratings.Average(r => (double)r.DefensiveContribution);
        trends["physicalStrength"] = ratings.Average(r => (double)r.PhysicalStrength);
        trends["behavior"] = ratings.Average(r => (double)r.Behavior);
        trends["overallPerformance"] = ratings.Average(r => (double)r.OverallPerformance);

        // Calculate trend direction (simple: compare first half vs second half)
        var midPoint = ratings.Count / 2;
        if (midPoint > 0)
        {
            var firstHalf = ratings.Take(midPoint);
            var secondHalf = ratings.Skip(midPoint);

            trends["passing_trend"] = secondHalf.Average(r => (double)r.Passing) - firstHalf.Average(r => (double)r.Passing);
            trends["shooting_trend"] = secondHalf.Average(r => (double)r.Shooting) - firstHalf.Average(r => (double)r.Shooting);
            trends["overall_trend"] = secondHalf.Average(r => (double)r.OverallPerformance) - firstHalf.Average(r => (double)r.OverallPerformance);
        }

        return trends;
    }

    private List<string> DetectInjuryRisks(List<Note> notes)
    {
        var injuryKeywords = new[] { "knee", "ankle", "hamstring", "groin", "shoulder", "back", "calf", "thigh" };
        var medicalNotes = notes.Where(n => n.Category?.ToLower().Contains("medical") == true).ToList();

        var risks = new List<string>();

        foreach (var note in medicalNotes)
        {
            var lowerDescription = note.Description.ToLower();
            foreach (var keyword in injuryKeywords)
            {
                if (lowerDescription.Contains(keyword))
                {
                    risks.Add($"Potential {keyword} issue mentioned in medical notes");
                    break;
                }
            }
        }

        return risks.Distinct().ToList();
    }

    private string BuildAiPrompt(ProcessedPlayerData data, AiPlanGenerateRequest request)
    {
        var prompt = $@"
PLAYER PROFILE:
Name: {data.Player.FullName}
Age: {data.Age}
Position: {data.Player.PositionCode}
Nationality: {data.Player.Nationality}

PLAN PREFERENCES:
Skill Focus: {request.SkillType}
Current Level: {request.CurrentLevel}
Target Level: {request.TargetLevel}
Duration: {request.DurationWeeks} weeks
Training Days Per Week: {request.TrainingDaysPerWeek}
Session Duration: {request.SessionDurationMinutes} minutes
Has Injury: {(request.HasInjury ? "Yes" : "No")}
Injury Details: {(string.IsNullOrWhiteSpace(request.InjuryDetails) ? "N/A" : request.InjuryDetails)}

CURRENT RATINGS (Average):
Passing: {data.RatingTrends.GetValueOrDefault("passing", "N/A")}
Shooting: {data.RatingTrends.GetValueOrDefault("shooting", "N/A")}
Dribbling: {data.RatingTrends.GetValueOrDefault("dribbling", "N/A")}
Tactical Awareness: {data.RatingTrends.GetValueOrDefault("tacticalAwareness", "N/A")}
Defensive Contribution: {data.RatingTrends.GetValueOrDefault("defensiveContribution", "N/A")}
Physical Strength: {data.RatingTrends.GetValueOrDefault("physicalStrength", "N/A")}
Behavior: {data.RatingTrends.GetValueOrDefault("behavior", "N/A")}
Overall Performance: {data.RatingTrends.GetValueOrDefault("overallPerformance", "N/A")}

RATING TRENDS:
Passing Trend: {data.RatingTrends.GetValueOrDefault("passing_trend", "N/A")}
Shooting Trend: {data.RatingTrends.GetValueOrDefault("shooting_trend", "N/A")}
Overall Trend: {data.RatingTrends.GetValueOrDefault("overall_trend", "N/A")}

NOTES BY CATEGORY:
";

        foreach (var category in data.NotesByCategory)
        {
            prompt += $"{category.Key}: {string.Join("; ", category.Value)}\n";
        }

        prompt += "\nREVIEW COMMENTS:\n";
        prompt += string.Join("\n", data.Reviews);

        if (data.InjuryRisks.Any())
        {
            prompt += "\nINJURY RISKS DETECTED:\n";
            prompt += string.Join("\n", data.InjuryRisks);
        }

        if (data.PreviousPlanSummaries.Any())
        {
            prompt += "\nPREVIOUS AI PLANS:\n";
            for (int i = 0; i < data.PreviousPlanSummaries.Count; i++)
            {
                prompt += $"Plan {i + 1}: {data.PreviousPlanSummaries[i]}\n";
            }
        }

        prompt += "\nMODIFIERS:\n";
        if (data.Age > 30)
        {
            prompt += "- Age > 30: Implement slower progression, focus on maintenance\n";
        }
        if (data.InjuryRisks.Any())
        {
            prompt += "- Injury risks detected: Include preventive exercises and recovery focus\n";
        }

        prompt += "\nGenerate a comprehensive development plan following the strict JSON format.";

        return prompt;
    }

    private static AiPlanGenerateRequest NormalizeRequest(AiPlanGenerateRequest? request)
    {
        if (request == null)
        {
            return new AiPlanGenerateRequest
            {
                SkillType = "General Development",
                CurrentLevel = "Intermediate",
                TargetLevel = "Advanced",
                DurationWeeks = 4,
                TrainingDaysPerWeek = 4,
                SessionDurationMinutes = 60,
                HasInjury = false,
                InjuryDetails = null
            };
        }

        if (string.IsNullOrWhiteSpace(request.SkillType))
        {
            throw new ArgumentException("Skill type is required.");
        }

        if (request.HasInjury && string.IsNullOrWhiteSpace(request.InjuryDetails))
        {
            throw new ArgumentException("Injury details are required when injury is marked as true.");
        }

        return request;
    }

    private AiPlanResponse MapToResponse(PlayerAiPlan planEntity, AiPlanContent planContent)
    {
        return new AiPlanResponse
        {
            PlanId = planEntity.PlanId,
            PlayerId = planEntity.PlayerId,
            Plan = planContent,
            Version = planEntity.Version,
            CreatedAt = planEntity.CreatedAt,
            SkillType = planEntity.SkillType,
            CurrentLevel = planEntity.CurrentLevel,
            TargetLevel = planEntity.TargetLevel,
            DurationWeeks = planEntity.DurationWeeks,
            TrainingDaysPerWeek = planEntity.TrainingDaysPerWeek,
            SessionDurationMinutes = planEntity.SessionDurationMinutes,
            HasInjury = planEntity.HasInjury,
            InjuryDetails = planEntity.InjuryDetails,
            PdfPath = planEntity.PdfPath
        };
    }

    private async Task<string> CreateAiPlanPdfAsync(Player1 player, string planId, int version, AiPlanContent planContent, AiPlanGenerateRequest request)
    {
        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var safePlayerId = SanitizePathPart(player.PlayerId);
        var folderPath = Path.Combine(webRoot, "ai-plans", safePlayerId);
        Directory.CreateDirectory(folderPath);

        var fileName = $"{planId}.pdf";
        var filePath = Path.Combine(folderPath, fileName);

        var lines = new List<string>
        {
            $"Player: {player.FullName}",
            $"Plan Version: {version}",
            $"Generated On: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
            $"Skill Focus: {request.SkillType}",
            $"Level: {request.CurrentLevel} -> {request.TargetLevel}",
            $"Duration: {request.DurationWeeks} weeks",
            $"Training Days/Week: {request.TrainingDaysPerWeek}",
            $"Session Duration: {request.SessionDurationMinutes} mins",
            $"Has Injury: {(request.HasInjury ? "Yes" : "No")}",
            $"Injury Details: {(string.IsNullOrWhiteSpace(request.InjuryDetails) ? "N/A" : request.InjuryDetails)}",
            "",
            "Summary:",
            planContent.Summary
        };

        var pdfBytes = BuildSimplePdf(lines);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return $"/ai-plans/{safePlayerId}/{fileName}";
    }

    private static string SanitizePathPart(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static byte[] BuildSimplePdf(List<string> lines)
    {
        var escapedLines = lines.Select(EscapePdfText).ToList();
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 11 Tf");
        contentBuilder.AppendLine("50 800 Td");

        for (var i = 0; i < escapedLines.Count; i++)
        {
            if (i > 0)
            {
                contentBuilder.AppendLine("0 -14 Td");
            }
            contentBuilder.AppendLine($"({escapedLines[i]}) Tj");
        }

        contentBuilder.AppendLine("ET");
        var contentStream = contentBuilder.ToString();
        var contentLength = Encoding.ASCII.GetByteCount(contentStream);

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {contentLength} >>\nstream\n{contentStream}endstream\nendobj\n"
        };

        var pdfBuilder = new StringBuilder();
        pdfBuilder.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };

        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdfBuilder.ToString()));
            pdfBuilder.Append(obj);
        }

        var xrefStart = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());
        pdfBuilder.Append($"xref\n0 {objects.Count + 1}\n");
        pdfBuilder.Append("0000000000 65535 f \n");

        for (var i = 1; i < offsets.Count; i++)
        {
            pdfBuilder.Append($"{offsets[i]:D10} 00000 n \n");
        }

        pdfBuilder.Append("trailer\n");
        pdfBuilder.Append($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        pdfBuilder.Append("startxref\n");
        pdfBuilder.Append($"{xrefStart}\n");
        pdfBuilder.Append("%%EOF");

        return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
    }

    private static string EscapePdfText(string text)
    {
        return (text ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private AiPlanContent ParseAiResponse(string aiResponse)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
            {
                throw new JsonException("No JSON found in AI response");
            }

            var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            
            // Parse the JSON to fix common issues
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var fixedJson = FixJsonArrays(jsonDoc);
            
            // Use case-insensitive deserialization to handle different property naming conventions
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
            
            var planContent = JsonSerializer.Deserialize<AiPlanContent>(fixedJson, options);

            if (planContent == null)
            {
                throw new JsonException("Failed to deserialize AI response");
            }

            return planContent;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", aiResponse);
            throw new Exception("AI service returned invalid JSON response", ex);
        }
    }

    private string FixJsonArrays(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        var fixedObject = new Dictionary<string, object>();

        // Define expected array properties
        var arrayProperties = new[] { 
            "strengths", "weaknesses", "injury_risks", "improvements_from_last_plan", 
            "performance_tracking", "recommendations" 
        };
        var objectProperties = new[] { "timeline_weeks", "skill_plan", "weekly_schedule" };

        foreach (var property in root.EnumerateObject())
        {
            var key = property.Name.ToLower();
            var value = property.Value;

            if (arrayProperties.Contains(key))
            {
                // Ensure array properties are arrays
                if (value.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in value.EnumerateArray())
                    {
                        list.Add(item.GetString() ?? item.ToString());
                    }
                    fixedObject[property.Name] = list;
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    // Convert string to array with single item
                    fixedObject[property.Name] = new List<string> { value.GetString() ?? "" };
                }
                else
                {
                    // Convert anything else to empty array
                    fixedObject[property.Name] = new List<string>();
                }
            }
            else if (objectProperties.Contains(key))
            {
                // Ensure object properties are objects
                if (value.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var objProp in value.EnumerateObject())
                    {
                        if (objProp.Value.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<string>();
                            foreach (var item in objProp.Value.EnumerateArray())
                            {
                                list.Add(item.GetString() ?? item.ToString());
                            }
                            dict[objProp.Name] = list;
                        }
                        else
                        {
                            dict[objProp.Name] = objProp.Value.GetString() ?? objProp.Value.ToString();
                        }
                    }
                    fixedObject[property.Name] = dict;
                }
                else
                {
                    fixedObject[property.Name] = new Dictionary<string, object>();
                }
            }
            else if (value.ValueKind == JsonValueKind.Object)
            {
                // Recursively fix nested objects
                var nestedJson = JsonSerializer.Serialize(value);
                var nestedDoc = JsonDocument.Parse(nestedJson);
                fixedObject[property.Name] = JsonSerializer.Deserialize<Dictionary<string, object>>(FixJsonArrays(nestedDoc))!;
            }
            else
            {
                // Keep other values as is
                fixedObject[property.Name] = JsonSerializer.Deserialize<object>(value.GetRawText())!;
            }
        }

        return JsonSerializer.Serialize(fixedObject);
    }
}

internal class ProcessedPlayerData
{
    public Player1 Player { get; set; } = null!;
    public int Age { get; set; }
    public Dictionary<string, List<string>> NotesByCategory { get; set; } = new();
    public List<string> Reviews { get; set; } = new();
    public Dictionary<string, object> RatingTrends { get; set; } = new();
    public List<string> InjuryRisks { get; set; } = new();
    public List<string> PreviousPlanSummaries { get; set; } = new();
}