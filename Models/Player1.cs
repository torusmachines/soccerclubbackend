using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Models;

[Table("players", Schema = "stf")]
[Index("CurrentClubId", Name = "IX_players_current_club_id")]
public partial class Player1
{
    [Key]
    [Column("player_id")]
    [StringLength(50)]
    public string PlayerId { get; set; } = null!;

    [Column("full_name")]
    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Column("nationality")]
    [StringLength(100)]
    public string Nationality { get; set; } = null!;

    [Column("position_code")]
    [StringLength(10)]
    public string PositionCode { get; set; } = null!;

    [Column("preferred_foot")]
    [StringLength(10)]
    public string PreferredFoot { get; set; } = null!;

    [Column("height_cm")]
    public int HeightCm { get; set; }

    [Column("weight_kg")]
    public int WeightKg { get; set; }

    [Column("current_club_id")]
    [StringLength(50)]
    public string? CurrentClubId { get; set; }

    [Column("contract_start_date")]
    public DateOnly? ContractStartDate { get; set; }

    [Column("contract_end_date")]
    public DateOnly? ContractEndDate { get; set; }

    [Column("agent_name")]
    [StringLength(150)]
    public string? AgentName { get; set; } = null!;

    [Column("player_email")]
    [StringLength(255)]
    public string playerEmail { get; set; } = null!;

    [Column("agent_scout_id")]
    [StringLength(50)]
    public string? AgentScoutId { get; set; } = null!;

    [Column("contact_info")]
    [StringLength(255)]
    public string? ContactInfo { get; set; }

    [Column("profile_image_url")]
    [StringLength(500)]
    public string? ProfileImageUrl { get; set; }

    [Column("sport_id")]
    public int? SportId { get; set; }

    [Column("contract_start_with_coach")]
    public DateOnly? ContractStartWithCoach { get; set; }

    [Column("contract_end_with_coach")]
    public DateOnly? ContractEndWithCoach { get; set; }

    [Column("address_line1")]
    [StringLength(150)]
    public string? AddressLine1 { get; set; }

    [Column("address_line2")]
    [StringLength(150)]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [StringLength(100)]
    public string? State { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [Column("postal_code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Column("gender")]
    [StringLength(10)]
    public string? Gender { get; set; }

    [Column("place_of_birth")]
    [StringLength(100)]
    public string? PlaceOfBirth { get; set; }

    [Column("primary_language")]
    [StringLength(50)]
    public string? PrimaryLanguage { get; set; }

    [Column("secondary_language")]
    [StringLength(50)]
    public string? SecondaryLanguage { get; set; }

    [Column("profile_visibility")]
    public bool? ProfileVisibility { get; set; }

    [Column("phone_number")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("alternate_phone")]
    [StringLength(20)]
    public string? AlternatePhone { get; set; }

    [Column("emergency_contact_name")]
    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_number")]
    [StringLength(20)]
    public string? EmergencyContactNumber { get; set; }

    [Column("secondary_position")]
    [StringLength(50)]
    public string? SecondaryPosition { get; set; }

    [Column("jersey_number")]
    public int? JerseyNumber { get; set; }

    [Column("experience_years")]
    public int? ExperienceYears { get; set; }

    [Column("playing_level")]
    [StringLength(20)]
    public string? PlayingLevel { get; set; }

    [Column("dominant_side")]
    [StringLength(10)]
    public string? DominantSide { get; set; }

    [Column("fitness_level")]
    [StringLength(20)]
    public string? FitnessLevel { get; set; }

    [Column("injury_status")]
    [StringLength(20)]
    public string? InjuryStatus { get; set; }

    [Column("coach_email")]
    [StringLength(100)]
    public string? CoachEmail { get; set; }

    [Column("coach_phone")]
    [StringLength(20)]
    public string? CoachPhone { get; set; }

    [Column("user_status")]
    [StringLength(20)]
    public string UserStatus { get; set; } = "Approved";

    [Column("created_at")]
    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AgentScoutId")]
    [InverseProperty("Player1s")]
    public virtual Scout AgentScout { get; set; } = null!;

    [ForeignKey("CurrentClubId")]
    [InverseProperty("Player1s")]
    public virtual Club? CurrentClub { get; set; }

    [ForeignKey("SportId")]
    [InverseProperty("Player1s")]
    public virtual Sport? Sport { get; set; }

    [InverseProperty("Player")]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    [InverseProperty("Player")]
    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();

    [InverseProperty("Player")]
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    [InverseProperty("Player")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    [InverseProperty("Player")]
    public virtual ICollection<PlayerAiPlan> PlayerAiPlans { get; set; } = new List<PlayerAiPlan>();
    [InverseProperty("Player")]
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
