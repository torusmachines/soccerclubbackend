using System;
using System.Collections.Generic;

namespace FootballDashboardAPI.Models.Responses;

public class ClubDetailsResponse
{
    public ClubDto ClubDetails { get; set; } = null!;
    public IEnumerable<PlayerAtClubDto> PlayersAtClub { get; set; } = Array.Empty<PlayerAtClubDto>();
    public IEnumerable<ClubContactDto> AllContactsForClubs { get; set; } = Array.Empty<ClubContactDto>();
    public List<ClubNoteResponse> club_all_notes { get; set; } = new();
    public List<ClubDocumentResponse> club_all_documents { get; set; } = new();
    public List<ClubEmailResponse> club_all_emails { get; set; } = new();
    public List<PlayerTaskResponse> club_all_tasks { get; set; } = new();
    public List<PlayerDetailsTemplateResponse> clubDetailsTemplates { get; set; } = new();
}
