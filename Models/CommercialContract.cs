using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballDashboardAPI.Models
{
    [Table("commercial_contracts", Schema = "stf")]
    public class CommercialContract
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("sponsor_id")]
        public Guid SponsorId { get; set; }

        [Required]
        [Column("entity_type")]
        public string EntityType { get; set; } // "club" or "player"

        [Column("club_id")]
        [StringLength(50)]
        public string? ClubId { get; set; }

        [Column("player_id")]
        [StringLength(50)]
        public string? PlayerId { get; set; }

        [Required]
        [Column("contract_start_date")]
        public DateTime ContractStartDate { get; set; }

        [Required]
        [Column("contract_end_date")]
        public DateTime ContractEndDate { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("contract_details")]
        public string? ContractDetails { get; set; }

        [Column("document_path")]
        public string? DocumentPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SponsorId")]
        public Sponsor Sponsor { get; set; }

        // Note: We don't add FK to Club/Player since we're not modifying existing tables
    }
}