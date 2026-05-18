using System;
namespace FootballDashboardAPI.Models;

public class ClubDto
{
    public string ClubId { get; set; } = null!;
    public string ClubName { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? AddressLine { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ClubContactCount { get; set; }
}
