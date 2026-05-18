using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models
{
    [Table("sponsors", Schema = "stf")]
    public class Sponsor
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("company_name")]
        public string CompanyName { get; set; } = null!;

        [Column("contact_name")]
        public string? ContactName { get; set; }

        [Column("contact_email")]
        public string? ContactEmail { get; set; }

        [Column("contact_phone")]
        public string? ContactPhone { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        public ICollection<CommercialContract> Contracts { get; set; } = new List<CommercialContract>();

        [JsonIgnore]
        public ICollection<SponsorComment> Comments { get; set; } = new List<SponsorComment>();
    }
}