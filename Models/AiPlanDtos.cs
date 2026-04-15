using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models;

public class AiPlanRequest
{
    [Required]
    public string PlayerId { get; set; } = null!;
}

public class AiPlanGenerateRequest
{
    [Required]
    public string SkillType { get; set; } = null!;

    [Required]
    [RegularExpression("Beginner|Intermediate|Advanced")]
    public string CurrentLevel { get; set; } = null!;

    [Required]
    [RegularExpression("Beginner|Intermediate|Advanced")]
    public string TargetLevel { get; set; } = null!;

    [Range(1, 52)]
    public int DurationWeeks { get; set; }

    [Range(1, 7)]
    public int TrainingDaysPerWeek { get; set; }

    [Range(15, 180)]
    public int SessionDurationMinutes { get; set; }

    public bool HasInjury { get; set; }

    public string? InjuryDetails { get; set; }
}

public class AiPlanResponse
{
    public string PlanId { get; set; } = null!;
    public string PlayerId { get; set; } = null!;
    public AiPlanContent Plan { get; set; } = null!;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SkillType { get; set; }
    public string? CurrentLevel { get; set; }
    public string? TargetLevel { get; set; }
    public int? DurationWeeks { get; set; }
    public int? TrainingDaysPerWeek { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public bool? HasInjury { get; set; }
    public string? InjuryDetails { get; set; }
    public string? PdfPath { get; set; }
}

public class AiPlanHistoryResponse
{
    public List<AiPlanResponse> Plans { get; set; } = new List<AiPlanResponse>();
}

public class AiPlanContent
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = null!;

    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = new List<string>();

    [JsonPropertyName("weaknesses")]
    public List<string> Weaknesses { get; set; } = new List<string>();

    [JsonPropertyName("trend_analysis")]
    public string TrendAnalysis { get; set; } = null!;

    [JsonPropertyName("injury_risks")]
    public List<string> InjuryRisks { get; set; } = new List<string>();

    [JsonPropertyName("improvements_from_last_plan")]
    public List<string> ImprovementsFromLastPlan { get; set; } = new List<string>();

    [JsonPropertyName("timeline_weeks")]
    public Dictionary<string, string> TimelineWeeks { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("skill_plan")]
    public Dictionary<string, List<string>> SkillPlan { get; set; } = new Dictionary<string, List<string>>();

    [JsonPropertyName("weekly_schedule")]
    public Dictionary<string, List<string>> WeeklySchedule { get; set; } = new Dictionary<string, List<string>>();

    [JsonPropertyName("performance_tracking")]
    public List<string> PerformanceTracking { get; set; } = new List<string>();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new List<string>();
}