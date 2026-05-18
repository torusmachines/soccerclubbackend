using FootballDashboardAPI.Models;
using FootballDashboardAPI.Repositories;

namespace FootballDashboardAPI.Services;

public class ClubService : IClubService
{
    private readonly IClubRepository _clubRepository;

    public ClubService(IClubRepository clubRepository)
    {
        _clubRepository = clubRepository;
    }

    public async Task<IEnumerable<FootballDashboardAPI.Models.ClubDto>> GetAllClubsAsync()
    {
        return await _clubRepository.GetAllWithContactCountsAsync();
    }

    public async Task<FootballDashboardAPI.Models.ClubDto?> GetClubByIdAsync(string id)
    {
        return await _clubRepository.GetByIdWithContactCountAsync(id);
    }

    public async Task<FootballDashboardAPI.Models.Responses.ClubDetailsResponse?> GetClubDetailsAsync(string id)
    {
        return await _clubRepository.GetClubDetailsWithPlayersAsync(id);
    }

    public async Task<Club> CreateClubAsync(CreateClub createClubDto)
    {
        try
        {
            // Validate that required fields are not empty
            if (string.IsNullOrWhiteSpace(createClubDto.ClubName))
            {
                throw new InvalidOperationException("Club name is required.");
            }

            if (string.IsNullOrWhiteSpace(createClubDto.Country))
            {
                throw new InvalidOperationException("Country is required.");
            }

            // Validate unique club name
            var nameExists = await _clubRepository.ClubNameExistsAsync(createClubDto.ClubName);
            if (nameExists)
            {
                throw new InvalidOperationException($"Club with name '{createClubDto.ClubName}' already exists.");
            }

            var club = new Club
            {
             //   ClubId = Guid.NewGuid().ToString(),
                ClubName = createClubDto.ClubName,
                Country = createClubDto.Country,
                AddressLine = createClubDto.AddressLine,
                LogoUrl = createClubDto.LogoUrl,
                CreatedAt = DateTime.UtcNow
            };

            var createdClub = await _clubRepository.CreateAsync(club);
            return MapToDto(createdClub);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Unexpected error creating club: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw new InvalidOperationException("An error occurred while creating the club. Please check the server logs for details.", ex);
        }
    }

    public async Task<Club?> UpdateClubAsync(string id, UpdateClub updateClubDto)
    {
        var existingClub = await _clubRepository.GetByIdAsync(id);
        if (existingClub == null)
            return null;

        // Validate unique club name (excluding current club)
        if (await _clubRepository.ClubNameExistsAsync(updateClubDto.ClubName, id))
        {
            throw new InvalidOperationException($"Club with name '{updateClubDto.ClubName}' already exists.");
        }

        var club = new Club
        {
            ClubId = id,
            ClubName = updateClubDto.ClubName,
            Country = updateClubDto.Country,
            AddressLine = updateClubDto.AddressLine,
            LogoUrl = updateClubDto.LogoUrl,
            CreatedAt = existingClub.CreatedAt
        };

        var updatedClub = await _clubRepository.UpdateAsync(club);
        return updatedClub == null ? null : MapToDto(updatedClub);
    }

    public async Task<bool> DeleteClubAsync(string id)
    {
        return await _clubRepository.DeleteAsync(id);
    }

    private static Club MapToDto(Club club)
    {
        return new Club
        {
            ClubId = club.ClubId,
            ClubName = club.ClubName,
            Country = club.Country,
            AddressLine = club.AddressLine,
            LogoUrl = club.LogoUrl,
            CreatedAt = club.CreatedAt
        };
    }
}