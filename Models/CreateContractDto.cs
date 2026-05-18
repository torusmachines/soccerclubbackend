using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models
{
    public class CreateContractDto
    {
        private static readonly HashSet<string> AllowedPartyTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Player",
            "Club",
            "Company",
            "Coach"
        };

        private static readonly Dictionary<string, string> ContractTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PlayerClub"] = "PlayerClub",
            ["Player Club"] = "PlayerClub",
            ["Player-Club"] = "PlayerClub",
            ["Player ↔ Club"] = "PlayerClub",
            ["ClubCompany"] = "ClubCompany",
            ["Club Company"] = "ClubCompany",
            ["Club-Company"] = "ClubCompany",
            ["Club ↔ Company"] = "ClubCompany",
            ["PlayerCompany"] = "PlayerCompany",
            ["Player Company"] = "PlayerCompany",
            ["Player-Company"] = "PlayerCompany",
            ["Player ↔ Company"] = "PlayerCompany",
            ["PlayerCoach"] = "PlayerCoach",
            ["Player Coach"] = "PlayerCoach",
            ["Player-Coach"] = "PlayerCoach",
            ["Player ↔ Coach"] = "PlayerCoach"
        };

        [Required(ErrorMessage = "Party1Id is required")]
        [JsonPropertyName("party1Id")]
        public string? Party1Id { get; set; }

        [Required(ErrorMessage = "Party1Type is required")]
        [JsonPropertyName("party1Type")]
        public string? Party1Type { get; set; }

        [JsonPropertyName("party1Name")]
        public string? Party1Name { get; set; }

        [Required(ErrorMessage = "Party2Id is required")]
        [JsonPropertyName("party2Id")]
        public string? Party2Id { get; set; }

        [Required(ErrorMessage = "Party2Type is required")]
        [JsonPropertyName("party2Type")]
        public string? Party2Type { get; set; }

        [JsonPropertyName("party2Name")]
        public string? Party2Name { get; set; }

        [Required(ErrorMessage = "ContractType is required")]
        [JsonPropertyName("contractType")]
        public string? ContractType { get; set; }

        [Required(ErrorMessage = "StartDate is required")]
        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [Required(ErrorMessage = "EndDate is required")]
        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("contractDetails")]
        public string? ContractDetails { get; set; }

        [JsonPropertyName("documentPath")]
        public string? DocumentPath { get; set; }

        public (Contract? contract, List<string>? errors) ToContract()
        {
            var errors = new List<string>();

            if (!Guid.TryParse(Party1Id, out var party1Id))
                errors.Add("Party1Id must be a valid GUID");

            if (!Guid.TryParse(Party2Id, out var party2Id))
                errors.Add("Party2Id must be a valid GUID");

            if (string.IsNullOrWhiteSpace(Party1Type) || !AllowedPartyTypes.Contains(Party1Type.Trim()))
                errors.Add("Party1Type must be one of Player, Club, Company, Coach");

            if (string.IsNullOrWhiteSpace(Party2Type) || !AllowedPartyTypes.Contains(Party2Type.Trim()))
                errors.Add("Party2Type must be one of Player, Club, Company, Coach");

            if (Party1Type?.Trim().Equals(Party2Type?.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                errors.Add("Party1Type and Party2Type must be different");

            var normalizedContractType = string.IsNullOrWhiteSpace(ContractType)
                ? string.Empty
                : ContractType.Trim();

            string contractTypeValue = string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedContractType) || !ContractTypeMap.TryGetValue(normalizedContractType, out contractTypeValue))
                errors.Add("ContractType must be one of PlayerClub, ClubCompany, PlayerCompany, PlayerCoach");

            if (!TryParseDateAsUtc(StartDate, out var startDate))
                errors.Add("StartDate must be a valid date in ISO format");

            if (!TryParseDateAsUtc(EndDate, out var endDate))
                errors.Add("EndDate must be a valid date in ISO format");

            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(ExpiryDate))
            {
                if (!TryParseDateAsUtc(ExpiryDate, out var parsedExpiryDate))
                {
                    errors.Add("ExpiryDate must be a valid date in ISO format");
                }
                else
                {
                    expiryDate = parsedExpiryDate;
                }
            }

            if (errors.Any())
                return (null, errors);

            if (endDate < startDate)
                errors.Add("EndDate must be after or equal to StartDate");

            if (expiryDate.HasValue && expiryDate.Value < startDate)
                errors.Add("ExpiryDate must be the same as or after StartDate");

            if (errors.Any())
                return (null, errors);

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                Party1Id = party1Id,
                Party1Type = Party1Type!.Trim(),
                Party1Name = string.IsNullOrWhiteSpace(Party1Name) ? null : Party1Name.Trim(),
                Party2Id = party2Id,
                Party2Type = Party2Type!.Trim(),
                Party2Name = string.IsNullOrWhiteSpace(Party2Name) ? null : Party2Name.Trim(),
                ContractType = contractTypeValue ,
                StartDate = startDate,
                EndDate = endDate,
                ExpiryDate = expiryDate,
                ContractDetails = string.IsNullOrWhiteSpace(ContractDetails) ? null : ContractDetails.Trim(),
                DocumentPath = string.IsNullOrWhiteSpace(DocumentPath) ? null : DocumentPath.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            return (contract, null);
        }

        private static bool TryParseDateAsUtc(string? value, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                return true;
            }

            return false;
        }
    }
}
