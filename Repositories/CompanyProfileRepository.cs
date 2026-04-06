using FootballDashboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FootballDashboardAPI.Repositories
{
    public interface ICompanyProfileRepository
    {
        Task<CompanyProfile?> GetAsync();
        Task<CompanyProfile> UpsertAsync(CompanyProfileDto dto);
    }

    public class CompanyProfileRepository : ICompanyProfileRepository
    {
        private readonly Data.AppDbContext _context;
        public CompanyProfileRepository(Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyProfile?> GetAsync()
        {
            try
            {
                return await _context.CompanyProfiles.FirstOrDefaultAsync(cp => cp.Id == 1);
            }
            catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42703")
            {
                // Column is missing in schema; fallback to default single row profile
                return new CompanyProfile
                {
                    Id = 1,
                    CompanyName = string.Empty,
                    ShortName = string.Empty,
                    Tagline = string.Empty,
                    Description = string.Empty,
                    FoundedYear = null,
                    LogoUrl = string.Empty,
                    PrimaryColor = string.Empty,
                    Email = string.Empty,
                    PhoneNumber = string.Empty,
                    AlternatePhone = string.Empty,
                    AddressLine1 = string.Empty,
                    AddressLine2 = string.Empty,
                    AreaLocality = string.Empty,
                    City = string.Empty,
                    District = string.Empty,
                    State = string.Empty,
                    Country = string.Empty,
                    PostalCode = string.Empty,
                    OrganizationType = string.Empty,
                    SportType = string.Empty,
                    FacebookUrl = string.Empty,
                    InstagramUrl = string.Empty,
                    TwitterUrl = string.Empty,
                    LinkedinUrl = string.Empty,
                    YoutubeUrl = string.Empty,
                    ContractExpiringMonths = 6
                };
            }
        }

        public async Task<CompanyProfile> UpsertAsync(CompanyProfileDto dto)
        {
            try
            {
                var entity = await _context.CompanyProfiles.FirstOrDefaultAsync(cp => cp.Id == 1);
                if (entity == null)
                {
                    entity = new CompanyProfile { Id = 1 };
                    _context.CompanyProfiles.Add(entity);
                }
                // Map fields
                entity.CompanyName = dto.CompanyName;
                entity.ShortName = dto.ShortName;
                entity.Tagline = dto.Tagline;
                entity.Description = dto.Description;
                entity.FoundedYear = dto.FoundedYear;
                entity.LogoUrl = dto.LogoUrl;
                entity.PrimaryColor = dto.PrimaryColor;
                entity.Email = dto.Email;
                entity.PhoneNumber = dto.PhoneNumber;
                entity.AlternatePhone = dto.AlternatePhone;
                entity.AddressLine1 = dto.AddressLine1;
                entity.AddressLine2 = dto.AddressLine2;
                entity.AreaLocality = dto.AreaLocality;
                entity.City = dto.City;
                entity.District = dto.District;
                entity.State = dto.State;
                entity.Country = dto.Country;
                entity.PostalCode = dto.PostalCode;
                entity.OrganizationType = dto.OrganizationType;
                entity.SportType = dto.SportType;
                entity.FacebookUrl = dto.FacebookUrl;
                entity.InstagramUrl = dto.InstagramUrl;
                entity.TwitterUrl = dto.TwitterUrl;
                entity.LinkedinUrl = dto.LinkedinUrl;
                entity.YoutubeUrl = dto.YoutubeUrl;
                entity.ContractExpiringMonths = dto.ContractExpiringMonths > 0 ? dto.ContractExpiringMonths : 6;

                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42703")
            {
                // Skip persistence of missing column, but return a temporary object.
                return new CompanyProfile
                {
                    Id = 1,
                    CompanyName = dto.CompanyName,
                    ShortName = dto.ShortName,
                    Tagline = dto.Tagline,
                    Description = dto.Description,
                    FoundedYear = dto.FoundedYear,
                    LogoUrl = dto.LogoUrl,
                    PrimaryColor = dto.PrimaryColor,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    AlternatePhone = dto.AlternatePhone,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    AreaLocality = dto.AreaLocality,
                    City = dto.City,
                    District = dto.District,
                    State = dto.State,
                    Country = dto.Country,
                    PostalCode = dto.PostalCode,
                    OrganizationType = dto.OrganizationType,
                    SportType = dto.SportType,
                    FacebookUrl = dto.FacebookUrl,
                    InstagramUrl = dto.InstagramUrl,
                    TwitterUrl = dto.TwitterUrl,
                    LinkedinUrl = dto.LinkedinUrl,
                    YoutubeUrl = dto.YoutubeUrl,
                    ContractExpiringMonths = dto.ContractExpiringMonths > 0 ? dto.ContractExpiringMonths : 6
                };
            }
        }
    }
}