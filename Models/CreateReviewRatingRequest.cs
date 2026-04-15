using System;

namespace FootballDashboardAPI.Models;

public class CreateReviewRatingRequest
{
    public string? ReviewId { get; set; }
    public decimal Passing { get; set; }
    public decimal Shooting { get; set; }
    public decimal Dribbling { get; set; }
    public decimal TacticalAwareness { get; set; }
    public decimal DefensiveContribution { get; set; }
    public decimal PhysicalStrength { get; set; }
    public decimal Behavior { get; set; }
    public decimal OverallPerformance { get; set; }
}
