using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballDashboardAPI.Models;

[Table("UserConsentHistory", Schema = "auth")]
public class UserConsentHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public bool ConsentGiven { get; set; }

    [Required]
    [MaxLength(32)]
    public string ConsentVersion { get; set; } = "v1.0";

    [Required]
    [MaxLength(32)]
    public string ConsentSource { get; set; } = "signup";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
