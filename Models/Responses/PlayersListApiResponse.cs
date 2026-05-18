namespace FootballDashboardAPI.Models.Responses;

public class PlayersListApiResponse
{
    public IEnumerable<PlayerListResponse> Players { get; set; } = Enumerable.Empty<PlayerListResponse>();
    public PlayersOtherDataResponse OtherData { get; set; } = new();
}

public class PlayersOtherDataResponse
{
    public IEnumerable<ScoutOptionResponse> ScoutOptions { get; set; } = Enumerable.Empty<ScoutOptionResponse>();
    public IEnumerable<PositionOptionResponse> PositionOptions { get; set; } = Enumerable.Empty<PositionOptionResponse>();
    public IEnumerable<SportOptionResponse> SportsOptions { get; set; } = Enumerable.Empty<SportOptionResponse>();
    public bool LoggedInScoutIsShowPlayer { get; set; }
}

public class ScoutOptionResponse
{
    public string ScoutId { get; set; } = string.Empty;
    public string ScoutName { get; set; } = string.Empty;
}

public class PositionOptionResponse
{
    public string PositionId { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
}

public class SportOptionResponse
{
    public int SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
}