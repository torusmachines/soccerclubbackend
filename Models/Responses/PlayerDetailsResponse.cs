namespace FootballDashboardAPI.Models.Responses;

public class PlayerDetailsResponse
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PreferredFoot { get; set; } = string.Empty;
    public int HeightCm { get; set; }
    public int WeightKg { get; set; }
    public string ContactInfo { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string? ScoutId { get; set; }
    public string? ScoutName { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public DateOnly? ContractStartWithCoach { get; set; }
    public DateOnly? ContractEndWithCoach { get; set; }
    public string ContractStatus { get; set; } = string.Empty;
    public int? SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public decimal OverallRating { get; set; }
    public string? CurrentClubId { get; set; }
    public string? CurrentClubName { get; set; }
    // Extended profile fields
    public string PlayerEmail { get; set; } = string.Empty;
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
    [System.Text.Json.Serialization.JsonPropertyName("secondary_position")]
    public string? SecondaryPosition { get; set; }
    public int? JerseyNumber { get; set; }
    public int? ExperienceYears { get; set; }
    public string? PlayingLevel { get; set; }
    public string? DominantSide { get; set; }
    public string? FitnessLevel { get; set; }
    public string? InjuryStatus { get; set; }
    public string? CoachEmail { get; set; }
    public string? CoachPhone { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
    public List<ActivityRatingResponse> ActivityRatings { get; set; } = new();
    public PlayerSportDetailsResponse player_sport_details { get; set; } = new();
    public List<PlayerReviewResponse> player_all_review { get; set; } = new();
    public List<PlayerNoteResponse> player_all_notes { get; set; } = new();
    public List<PlayerDocumentResponse> player_all_documents { get; set; } = new();
    public List<PlayerTaskResponse> player_all_tasks { get; set; } = new();
    public List<PlayerEmailResponse> player_all_emails { get; set; } = new();
    public List<PlayerCommercialContractResponse> player_all_commercial_contracts { get; set; } = new();
    public PlayerDetailsOtherDataResponse playerDetailsOtherData { get; set; } = new();
}

public class PlayerDetailsOtherDataResponse
{
    public List<PlayerDetailsPositionOptionResponse> playerDetailsPositionData { get; set; } = new();
    public List<PlayerDetailsSportOptionResponse> playerDetailsSportsData { get; set; } = new();
    public List<PlayerDetailsScoutOptionResponse> playerDetailsScoutData { get; set; } = new();
    public List<PlayerDetailsClubOptionResponse> playerDetailsClubData { get; set; } = new();
    public List<PlayerDetailsTemplateResponse> playerDetailsTemplate { get; set; } = new();
}

public class PlayerDetailsTemplateResponse
{
    public string templateId { get; set; } = string.Empty;
    public string templateName { get; set; } = string.Empty;
    public string templateType { get; set; } = string.Empty;
    public string subject { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
}

public class PlayerDetailsPositionOptionResponse
{
    public string positionId { get; set; } = string.Empty;
    public string positionName { get; set; } = string.Empty;
    public string positionCode { get; set; } = string.Empty;
    public int? positionForSportId { get; set; }
}

public class PlayerDetailsSportOptionResponse
{
    public int sportId { get; set; }
    public string sportName { get; set; } = string.Empty;
}

public class PlayerDetailsScoutOptionResponse
{
    public string scoutId { get; set; } = string.Empty;
    public string scoutName { get; set; } = string.Empty;
}

public class PlayerDetailsClubOptionResponse
{
    public string clubId { get; set; } = string.Empty;
    public string clubName { get; set; } = string.Empty;
}

public class PlayerSportDetailsResponse
{
    public string sport_name { get; set; } = string.Empty;
    public int? sport_id { get; set; }
    public List<PlayerSportEntityResponse> sport_entity { get; set; } = new();
}

public class PlayerSportEntityResponse
{
    public int entity_id { get; set; }
    public string entity_name { get; set; } = string.Empty;
}