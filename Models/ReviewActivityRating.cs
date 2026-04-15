using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballDashboardAPI.Models;

[Table("review_activity_ratings", Schema = "stf")]
public partial class ReviewActivityRating
{
    [Key]
    [Column("review_activity_rating_id")]
    public int ReviewActivityRatingId { get; set; }

    [Column("review_id")]
    [StringLength(50)]
    public string ReviewId { get; set; } = null!;

    [Column("activity_id")]
    public int ActivityId { get; set; }

    [Column("rating", TypeName = "decimal(3,1)")]
    public decimal Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("rating_followup_date")]
    public DateTime? RatingFollowupDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
