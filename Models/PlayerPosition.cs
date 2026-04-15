namespace FootballDashboardAPI.Models;

public class PlayerPosition
{
    public string PositionId { get; set; } = Guid.NewGuid().ToString();
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SportId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreatePlayerPosition
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SportId { get; set; }
}

public class UpdatePlayerPosition
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SportId { get; set; }
}
