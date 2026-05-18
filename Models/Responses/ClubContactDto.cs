using System;

namespace FootballDashboardAPI.Models.Responses;

public class ClubContactDto
{
    public string ClubContactId { get; set; } = null!;
    public string ClubId { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
