
namespace FootballDashboardAPI.Models.Entities;

public class Player
{
    public string PlayerId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Nationality { get; set; }

    public string? PositionCode { get; set; }

    public string? PreferredFoot { get; set; }

    public int? HeightCm { get; set; }

    public int? WeightKg { get; set; }

    public string? CurrentClubId { get; set; }

    public DateOnly? ContractStartDate { get; set; }

    public DateOnly? ContractEndDate { get; set; }

    public string? AgentName { get; set; }

    public string? PlayerEmail { get; set; }

    public string? AgentScoutId { get; set; }

    public string? ContactInfo { get; set; }

    public string? ProfileImageUrl { get; set; }

    public int? SportId { get; set; }

    // New contact / profile fields
    public string? Gender { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? SecondaryLanguage { get; set; }
    public bool? ProfileVisibility { get; set; }

    public string? PhoneNumber { get; set; }
    public string? AlternatePhone { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactNumber { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }

    public string? SecondaryPosition { get; set; }
    public int? JerseyNumber { get; set; }
    public int? ExperienceYears { get; set; }
    public string? PlayingLevel { get; set; }

    public string? DominantSide { get; set; }
    public string? FitnessLevel { get; set; }
    public string? InjuryStatus { get; set; }

    public string? CoachEmail { get; set; }
    public string? CoachPhone { get; set; }

    public DateOnly? ContractStartWithCoach { get; set; }

    public DateOnly? ContractEndWithCoach { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}








//namespace FootballDashboardAPI.Models.NewFolder2
//{
//    public class Player
//    {
//    }
//}
