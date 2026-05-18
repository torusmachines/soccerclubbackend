using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballDashboardAPI.Models
{
    [Table("contracts", Schema = "stf")]
    public class Contract
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("party1_id")]
        public Guid Party1Id { get; set; }

        [Required]
        [Column("party1_type")]
        [StringLength(50)]
        public string Party1Type { get; set; } = string.Empty;

        [Column("party1_name")]
        [StringLength(250)]
        public string? Party1Name { get; set; }

        [Required]
        [Column("party2_id")]
        public Guid Party2Id { get; set; }

        [Required]
        [Column("party2_type")]
        [StringLength(50)]
        public string Party2Type { get; set; } = string.Empty;

        [Column("party2_name")]
        [StringLength(250)]
        public string? Party2Name { get; set; }

        [Required]
        [Column("contract_type")]
        [StringLength(50)]
        public string ContractType { get; set; } = string.Empty;

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("contract_details")]
        public string? ContractDetails { get; set; }

        [Column("document_path")]
        public string? DocumentPath { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
