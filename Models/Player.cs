using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FootballDashboardAPI.Models;

[Table("players", Schema = "public")]
public class Player
{
    [Column("id")]
    public long Id { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = null!;

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("nationality")]
    public string? Nationality { get; set; }

    [Column("player_position")]
    public string? Position { get; set; }

    [Column("preferred_foot")]
    public string? PreferredFoot { get; set; }

    [Column("height_cm")]
    public int? HeightCm { get; set; }

    [Column("weight_kg")]
    public int? WeightKg { get; set; }

    [Column("current_club")]
    public string? CurrentClub { get; set; }

    [Column("contract_start")]
    public DateOnly? ContractStart { get; set; }

    [Column("contract_end")]
    public DateOnly? ContractEnd { get; set; }

    [Column("contract_status")]
    public string? ContractStatus { get; set; }

    [Column("agent_name")]
    public string? AgentName { get; set; }

    [Column("agent_contact")]
    public string? contact_info { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string? agent_scout_id { get; set; }

    [NotMapped]
    public string? profileImage { get; set; }

    [JsonPropertyName("player_email")]
    [NotMapped]
    public string? PlayerEmail { get; set; }

    [NotMapped]
    public int? SportId { get; set; }

    [NotMapped]
    public string? SportName { get; set; }

    [NotMapped]
    public DateOnly? ContractStartWithCoach { get; set; }

    [NotMapped]
    public DateOnly? ContractEndWithCoach { get; set; }

    [NotMapped]
    public string? AddressLine1 { get; set; }

    [NotMapped]
    public string? AddressLine2 { get; set; }

    /// <summary>Pending | Approved | Rejected</summary>
    [NotMapped]
    public string? UserStatus { get; set; }
}

public class CreatePlayer
{
    public string FullName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Position { get; set; }
    public string? PreferredFoot { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
    public string? CurrentClub { get; set; }
    public DateOnly? ContractStart { get; set; }
    public DateOnly? ContractEnd { get; set; }
    public string? ContractStatus { get; set; }
    public string? AgentName { get; set; }
    public string? AgentContact { get; set; }

    [JsonPropertyName("agent_scout_id")] // FIX: maps snake_case from frontend
    public string? AgentScoutId { get; set; }

    [JsonPropertyName("contact_info")]   // FIX: also add this since frontend sends contact_info
    public string? ContactInfo { get; set; }

    [JsonPropertyName("profileImage")] 
    public string? ProfileImage { get; set; }

    [JsonPropertyName("player_email")]
    public string? PlayerEmail { get; set; }

    public int? SportId { get; set; }
    public DateOnly? ContractStartWithCoach { get; set; }
    public DateOnly? ContractEndWithCoach { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
}

public class UpdatePlayer
{
    public string FullName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Position { get; set; }
    public string? PreferredFoot { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
    public string? CurrentClub { get; set; }
    public DateOnly? ContractStart { get; set; }
    public DateOnly? ContractEnd { get; set; }
    public string? ContractStatus { get; set; }
    public string? AgentName { get; set; }
    public string? AgentContact { get; set; }

    [JsonPropertyName("agent_scout_id")]
    public string? AgentScoutId { get; set; }

    [JsonPropertyName("contact_info")]
    public string? ContactInfo { get; set; }

    [JsonPropertyName("profileImage")]  
    public string? ProfileImage { get; set; }

    public int? SportId { get; set; }
    public DateOnly? ContractStartWithCoach { get; set; }
    public DateOnly? ContractEndWithCoach { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
}


