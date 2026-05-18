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
    public DbSet<SponsorComment> SponsorComments { get; set; }
    public DbSet<CommercialContract> CommercialContracts { get; set; }
    public DbSet<Sport> Sports { get; set; }
    public DbSet<SportActivity> SportActivities { get; set; }
    public DbSet<UserConsentHistory> UserConsentHistories { get; set; }

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

        builder.Entity<SponsorComment>()
            .ToTable("sponsor_comments", "stf")
            .HasKey(c => c.CommentId);
        builder.Entity<SponsorComment>().Property(c => c.Comment).IsRequired();
        builder.Entity<SponsorComment>().Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Entity<SponsorComment>().Property(c => c.IsAdminComment).HasDefaultValue(true);
        builder.Entity<SponsorComment>().Property(c => c.IsDeleted).HasDefaultValue(false);
        builder.Entity<SponsorComment>()
            .HasOne(c => c.Sponsor)
            .WithMany(s => s.Comments)
            .HasForeignKey(c => c.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);

        // CommercialContract configuration - using stf schema
        builder.Entity<CommercialContract>()
            .ToTable("commercial_contracts", "stf")
            .HasKey(c => c.Id);
        builder.Entity<CommercialContract>().Property(c => c.EntityType).IsRequired();
        builder.Entity<CommercialContract>().Property(c => c.ContractStartDate).IsRequired();
        builder.Entity<CommercialContract>().Property(c => c.ContractEndDate).IsRequired();
        builder.Entity<CommercialContract>().HasOne(c => c.Sponsor).WithMany(s => s.Contracts).HasForeignKey(c => c.SponsorId);

        // Sport configuration - using stf schema
        builder.Entity<Sport>()
            .ToTable("sports", "stf")
            .HasKey(s => s.SportId);
        builder.Entity<Sport>().Property(s => s.SportId).ValueGeneratedOnAdd();
        builder.Entity<Sport>().Property(s => s.SportName).IsRequired();

        // SportActivity configuration - using stf schema
        builder.Entity<SportActivity>()
            .ToTable("sport_activities", "stf")
            .HasKey(sa => sa.ActivityId);
        builder.Entity<SportActivity>().Property(sa => sa.ActivityId).ValueGeneratedOnAdd();
        builder.Entity<SportActivity>().Property(sa => sa.ActivityName).IsRequired();
        builder.Entity<SportActivity>().HasOne(sa => sa.Sport).WithMany(s => s.SportActivities).HasForeignKey(sa => sa.SportId);

        builder.Entity<UserConsentHistory>()
            .ToTable("UserConsentHistory", "auth")
            .HasKey(x => x.Id);

        builder.Entity<UserConsentHistory>()
            .HasOne(x => x.User)
            .WithMany(u => u.ConsentHistory)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserConsentHistory>()
            .HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}