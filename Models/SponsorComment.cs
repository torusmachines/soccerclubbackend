using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballDashboardAPI.Models
{
    [Table("sponsor_comments", Schema = "stf")]
    public class SponsorComment
    {
        [Key]
        [Column("comment_id")]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("sponsor_id")]
        public Guid SponsorId { get; set; }

        [Required]
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        [Column("created_by_user_id")]
        public string? CreatedByUserId { get; set; }

        [Column("created_by_name")]
        public string? CreatedByName { get; set; }

        [Column("created_by_role")]
        public string? CreatedByRole { get; set; }

        [Column("is_admin_comment")]
        public bool IsAdminComment { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(SponsorId))]
        public Sponsor? Sponsor { get; set; }
    }
}