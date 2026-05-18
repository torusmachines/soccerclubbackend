namespace FootballDashboardAPI.Models.Responses;

public class ClubDocumentResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string FileSizeLabel { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public bool IsVisibleToPlayer { get; set; }
}
