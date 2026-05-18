using System;

namespace FootballDashboardAPI.Models.Responses;

public class PlayerAtClubDto
{
    public string PlayerId { get; set; } = null!;
    public string PlayerName { get; set; } = null!;
    public string? Position { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public string? Nationality { get; set; }
}
