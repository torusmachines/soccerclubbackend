namespace FootballDashboardAPI.Models.Responses;

public class TaskConfigrationResponse
{
    public IEnumerable<TaskClubOptionResponse> allClubsForTask { get; set; } = Enumerable.Empty<TaskClubOptionResponse>();
    public IEnumerable<TaskScoutOptionResponse> allScoutForTask { get; set; } = Enumerable.Empty<TaskScoutOptionResponse>();
    public IEnumerable<TaskPlayerOptionResponse> allPlayerForTask { get; set; } = Enumerable.Empty<TaskPlayerOptionResponse>();
}

public class TaskClubOptionResponse
{
    public string ClubId { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
}

public class TaskScoutOptionResponse
{
    public string ScoutId { get; set; } = string.Empty;
    public string ScoutName { get; set; } = string.Empty;
}

public class TaskPlayerOptionResponse
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int? SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
}
