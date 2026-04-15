using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Models;

[Table("player_ai_plans", Schema = "stf")]
[Index("PlayerId", "Version", Name = "IX_player_ai_plans_player_version", IsDescending = new[] { false, true })]
[Index("CreatedAt", Name = "IX_player_ai_plans_created_at", IsDescending = new[] { true })]
public partial class PlayerAiPlan
{
    [Key]
    [Column("plan_id")]
    [StringLength(50)]
    public string PlanId { get; set; } = null!;

    [Column("player_id")]
    [StringLength(50)]
    public string PlayerId { get; set; } = null!;

    [Column("plan_json")]
    public string PlanJson { get; set; } = null!;

    [Column("raw_text")]
    public string? RawText { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("skill_type")]
    [StringLength(50)]
    public string? SkillType { get; set; }

    [Column("current_level")]
    [StringLength(20)]
    public string? CurrentLevel { get; set; }

    [Column("target_level")]
    [StringLength(20)]
    public string? TargetLevel { get; set; }

    [Column("duration_weeks")]
    public int? DurationWeeks { get; set; }

    [Column("training_days_per_week")]
    public int? TrainingDaysPerWeek { get; set; }

    [Column("session_duration_minutes")]
    public int? SessionDurationMinutes { get; set; }

    [Column("has_injury")]
    public bool? HasInjury { get; set; }

    [Column("injury_details")]
    public string? InjuryDetails { get; set; }

    [Column("pdf_path")]
    [StringLength(500)]
    public string? PdfPath { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("PlayerId")]
    [InverseProperty("PlayerAiPlans")]
    public virtual Player1? Player { get; set; }
}