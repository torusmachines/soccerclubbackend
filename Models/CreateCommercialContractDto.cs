using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models
{
    /// <summary>
    /// DTO for creating commercial contracts from the client
    /// Handles string-to-GUID and string-to-DateTime conversions
    /// </summary>
    public class CreateCommercialContractDto
    {
        [Required(ErrorMessage = "SponsorId is required")]
        [JsonPropertyName("sponsorId")]
        public string? SponsorId { get; set; }

        [Required(ErrorMessage = "EntityType is required")]
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; } // "club" or "player"

        [JsonPropertyName("clubId")]
        public string? ClubId { get; set; }

        [JsonPropertyName("playerId")]
        public string? PlayerId { get; set; }

        [Required(ErrorMessage = "ContractStartDate is required")]
        [JsonPropertyName("contractStartDate")]
        public string? ContractStartDate { get; set; }

        [Required(ErrorMessage = "ContractEndDate is required")]
        [JsonPropertyName("contractEndDate")]
        public string? ContractEndDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("contractDetails")]
        public string? ContractDetails { get; set; }

        [JsonPropertyName("documentPath")]
        public string? DocumentPath { get; set; }

        /// <summary>
        /// Convert DTO to CommercialContract model with proper type conversions
        /// </summary>
        public (CommercialContract? contract, List<string>? errors) ToCommercialContract()
        {
            var errors = new List<string>();

            // Validate and convert SponsorId
            if (!Guid.TryParse(SponsorId, out var sponsorId))
                errors.Add("SponsorId must be a valid GUID");

            // Validate EntityType
            if (string.IsNullOrWhiteSpace(EntityType) || (EntityType != "club" && EntityType != "player"))
                errors.Add("EntityType must be 'club' or 'player'");

            // Validate and convert dates to UTC
            if (!TryParseDateAsUtc(ContractStartDate, out var startDate))
                errors.Add("ContractStartDate must be a valid date");

            if (!TryParseDateAsUtc(ContractEndDate, out var endDate))
                errors.Add("ContractEndDate must be a valid date");

            // Validate EntityType-specific requirements
            if (EntityType == "club")
            {
                if (string.IsNullOrWhiteSpace(ClubId))
                    errors.Add("ClubId is required for club contracts");
            }

            if (EntityType == "player")
            {
                if (string.IsNullOrWhiteSpace(PlayerId))
                    errors.Add("PlayerId is required for player contracts");
            }

            if (errors.Any())
                return (null, errors);

            // All validations passed, create the contract
            var contract = new CommercialContract
            {
                Id = Guid.NewGuid(),
                SponsorId = sponsorId,
                EntityType = EntityType!,
                ClubId = EntityType == "club" ? ClubId : null,
                PlayerId = EntityType == "player" ? PlayerId : null,
                ContractStartDate = startDate,
                ContractEndDate = endDate,
                ExpiryDate = TryParseDateAsUtc(ExpiryDate, out var expiryDate) ? expiryDate : null,
                ContractDetails = ContractDetails,
                DocumentPath = DocumentPath,
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
