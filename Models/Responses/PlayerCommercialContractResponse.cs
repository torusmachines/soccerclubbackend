namespace FootballDashboardAPI.Models.Responses;

public class PlayerCommercialContractResponse
{
    public Guid Id { get; set; }
    public Guid SponsorId { get; set; }

    public string SponsorCompanyName { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;
    public string ClubId { get; set; } = string.Empty;

    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string ContractStatus { get; set; } = string.Empty;

    public string ContractDetails { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}