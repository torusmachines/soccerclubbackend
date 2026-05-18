using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Services;

public interface IClubService
{
    Task<IEnumerable<FootballDashboardAPI.Models.ClubDto>> GetAllClubsAsync();
    Task<FootballDashboardAPI.Models.ClubDto?> GetClubByIdAsync(string id);
    Task<FootballDashboardAPI.Models.Responses.ClubDetailsResponse?> GetClubDetailsAsync(string id);
    Task<Club> CreateClubAsync(CreateClub createClubDto);
    Task<Club?> UpdateClubAsync(string id, UpdateClub updateClubDto);
    Task<bool> DeleteClubAsync(string id);
}