namespace FootballDashboardAPI.Models
{
    public class ContractQueryParameters
    {
        public string? Search { get; set; }
        public string? ContractType { get; set; }
        public string? PartyType { get; set; }
        public Guid? PartyId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public string? ExpiryStatus { get; set; }
    }
}
