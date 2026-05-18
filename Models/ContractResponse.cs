namespace FootballDashboardAPI.Models
{
    public class ContractResponse
    {
        public Guid Id { get; set; }
        public Guid Party1Id { get; set; }
        public string Party1Type { get; set; } = string.Empty;
        public string? Party1Name { get; set; }
        public Guid Party2Id { get; set; }
        public string Party2Type { get; set; } = string.Empty;
        public string? Party2Name { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ContractDetails { get; set; }
        public string? DocumentPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ExpiryStatus { get; set; } = string.Empty;
    }
}
