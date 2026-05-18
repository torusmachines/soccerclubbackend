using Microsoft.AspNetCore.Http;

namespace FootballDashboardAPI.Models.Requests;

public class PlayerProfileImageUploadRequest
{
    public string PlayerId { get; set; } = string.Empty;

    public IFormFile? File { get; set; }
}
