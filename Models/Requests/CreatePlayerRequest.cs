using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models.Requests;

public class CreatePlayerRequest
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("preferredFoot")]
    public string? PreferredFoot { get; set; }

    [JsonPropertyName("heightCm")]
    public int? HeightCm { get; set; }

    [JsonPropertyName("weightKg")]
    public int? WeightKg { get; set; }

    [JsonPropertyName("currentClub")]
    public string? CurrentClub { get; set; }

    [JsonPropertyName("contractStart")]
    public string? ContractStart { get; set; }

    [JsonPropertyName("contractEnd")]
    public string? ContractEnd { get; set; }

    [JsonPropertyName("contractStartWithCoach")]
    public string? ContractStartWithCoach { get; set; }

    [JsonPropertyName("contractEndWithCoach")]
    public string? ContractEndWithCoach { get; set; }

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("agent_scout_id")]
    public string? AgentScoutId { get; set; }

    [JsonPropertyName("contact_info")]
    public string? ContactInfo { get; set; }

    [JsonPropertyName("player_email")]
    public string? PlayerEmail { get; set; }

    // Accept camelCase aliases from frontend when present
    [JsonPropertyName("playerEmail")]
    public string? PlayerEmailCamel { set { PlayerEmail = value; } }

    [JsonPropertyName("sportId")]
    public int? SportId { get; set; }

    [JsonPropertyName("sport_id")]
    public int? SportIdSnake { set { SportId = value; } }

    // Extended profile / contact fields (snake_case names to match frontend payload)
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("placeOfBirth")]
    public string? PlaceOfBirthCamel { set { PlaceOfBirth = value; } }

    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; set; }

    [JsonPropertyName("primaryLanguage")]
    public string? PrimaryLanguageCamel { set { PrimaryLanguage = value; } }

    [JsonPropertyName("secondary_language")]
    public string? SecondaryLanguage { get; set; }

    [JsonPropertyName("secondaryLanguage")]
    public string? SecondaryLanguageCamel { set { SecondaryLanguage = value; } }

    [JsonPropertyName("profile_visibility")]
    public bool? ProfileVisibility { get; set; }

    [JsonPropertyName("profileVisibility")]
    public bool? ProfileVisibilityCamel { set { ProfileVisibility = value; } }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumberCamel { set { PhoneNumber = value; } }

    [JsonPropertyName("alternate_phone")]
    public string? AlternatePhone { get; set; }

    [JsonPropertyName("alternatePhone")]
    public string? AlternatePhoneCamel { set { AlternatePhone = value; } }

    [JsonPropertyName("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [JsonPropertyName("emergencyContactName")]
    public string? EmergencyContactNameCamel { set { EmergencyContactName = value; } }

    [JsonPropertyName("emergency_contact_number")]
    public string? EmergencyContactNumber { get; set; }

    [JsonPropertyName("emergencyContactNumber")]
    public string? EmergencyContactNumberCamel { set { EmergencyContactNumber = value; } }

    [JsonPropertyName("address_line1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine1")]
    public string? AddressLine1Camel { set { AddressLine1 = value; } }

    [JsonPropertyName("address_line2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2Camel { set { AddressLine2 = value; } }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCodeCamel { set { PostalCode = value; } }

    [JsonPropertyName("secondary_position")]
    public string? SecondaryPosition { get; set; }

    [JsonPropertyName("secondaryPosition")]
    public string? SecondaryPositionCamel { set { SecondaryPosition = value; } }

    [JsonPropertyName("jersey_number")]
    public int? JerseyNumber { get; set; }

    [JsonPropertyName("jerseyNumber")]
    public int? JerseyNumberCamel { set { JerseyNumber = value; } }

    [JsonPropertyName("experience_years")]
    public int? ExperienceYears { get; set; }

    [JsonPropertyName("experienceYears")]
    public int? ExperienceYearsCamel { set { ExperienceYears = value; } }

    [JsonPropertyName("playing_level")]
    public string? PlayingLevel { get; set; }

    [JsonPropertyName("playingLevel")]
    public string? PlayingLevelCamel { set { PlayingLevel = value; } }

    [JsonPropertyName("dominant_side")]
    public string? DominantSide { get; set; }

    [JsonPropertyName("dominantSide")]
    public string? DominantSideCamel { set { DominantSide = value; } }

    [JsonPropertyName("fitness_level")]
    public string? FitnessLevel { get; set; }

    [JsonPropertyName("fitnessLevel")]
    public string? FitnessLevelCamel { set { FitnessLevel = value; } }

    [JsonPropertyName("injury_status")]
    public string? InjuryStatus { get; set; }

    [JsonPropertyName("injuryStatus")]
    public string? InjuryStatusCamel { set { InjuryStatus = value; } }

    [JsonPropertyName("coach_email")]
    public string? CoachEmail { get; set; }

    [JsonPropertyName("coachEmail")]
    public string? CoachEmailCamel { set { CoachEmail = value; } }

    [JsonPropertyName("coach_phone")]
    public string? CoachPhone { get; set; }

    [JsonPropertyName("coachPhone")]
    public string? CoachPhoneCamel { set { CoachPhone = value; } }
}
