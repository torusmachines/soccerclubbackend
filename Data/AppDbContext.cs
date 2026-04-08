using FootballDashboardAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FootballDashboardAPI.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CompanyProfile> CompanyProfiles { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<CommercialContract> CommercialContracts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("auth");

        builder.Entity<CompanyProfile>().HasKey(cp => cp.Id);
        builder.Entity<CompanyProfile>().HasData(new Models.CompanyProfile { Id = 1 });

        // Sponsor configuration - using stf schema
        builder.Entity<Sponsor>()
            .ToTable("sponsors", "stf")
            .HasKey(s => s.Id);
        builder.Entity<Sponsor>().Property(s => s.CompanyName).IsRequired();

        // CommercialContract configuration - using stf schema
        builder.Entity<CommercialContract>()
            .ToTable("commercial_contracts", "stf")
            .HasKey(c => c.Id);
        builder.Entity<CommercialContract>().Property(c => c.EntityType).IsRequired();
        builder.Entity<CommercialContract>().Property(c => c.ContractStartDate).IsRequired();
        builder.Entity<CommercialContract>().Property(c => c.ContractEndDate).IsRequired();
        builder.Entity<CommercialContract>().HasOne(c => c.Sponsor).WithMany(s => s.Contracts).HasForeignKey(c => c.SponsorId);
    }
}