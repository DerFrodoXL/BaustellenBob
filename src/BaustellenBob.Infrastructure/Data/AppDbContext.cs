using BaustellenBob.Domain.Entities;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<WorkReport> WorkReports => Set<WorkReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tenant
        builder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
        });

        // User
        builder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.Property(u => u.Name).HasMaxLength(200).IsRequired();
            e.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId);
            e.HasQueryFilter(u => u.TenantId == _tenantProvider.TenantId);
        });

        // Project
        builder.Entity<Project>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
            e.Property(b => b.Customer).HasMaxLength(200);
            e.Property(b => b.Address).HasMaxLength(500);
            e.HasOne(b => b.Tenant).WithMany(t => t.Projects).HasForeignKey(b => b.TenantId);
            e.HasQueryFilter(b => b.TenantId == _tenantProvider.TenantId);
        });

        // Photo
        builder.Entity<Photo>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FilePath).HasMaxLength(500).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.HasOne(p => p.Project).WithMany(b => b.Photos).HasForeignKey(p => p.ProjectId);
            e.HasOne(p => p.UploadedBy).WithMany().HasForeignKey(p => p.UploadedByUserId);
            e.HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);
        });

        // WorkReport
        builder.Entity<WorkReport>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Activity).HasMaxLength(1000).IsRequired();
            e.Property(w => w.Material).HasMaxLength(1000);
            e.Property(w => w.Hours).HasPrecision(5, 2);
            e.HasOne(w => w.Project).WithMany(b => b.WorkReports).HasForeignKey(w => w.ProjectId);
            e.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId);
            e.HasQueryFilter(w => w.TenantId == _tenantProvider.TenantId);
        });

        // Seed demo data
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000010");

        builder.Entity<Tenant>().HasData(new Tenant
        {
            Id = tenantId,
            Name = "Demo Handwerk GmbH",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Tier = Tier.Starter
        });

        builder.Entity<User>().HasData(new User
        {
            Id = userId,
            TenantId = tenantId,
            Name = "Demo Mitarbeiter",
            Email = "demo@baustellenbob.de",
            PasswordHash = string.Empty,
            Role = UserRole.Admin
        });

        builder.Entity<Project>().HasData(new Project
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000100"),
            TenantId = tenantId,
            Name = "Neubau Einfamilienhaus Müller",
            Customer = "Familie Müller",
            Address = "Musterstraße 12, 80331 München",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = ProjectStatus.Active,
            Description = "Elektroinstallation Neubau EFH"
        });

        // --- Second tenant for isolation testing ---
        var tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000020");

        builder.Entity<Tenant>().HasData(new Tenant
        {
            Id = tenant2Id,
            Name = "Sanitär Schmidt OHG",
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Tier = Tier.Starter
        });

        builder.Entity<User>().HasData(new User
        {
            Id = user2Id,
            TenantId = tenant2Id,
            Name = "Hans Schmidt",
            Email = "hans@sanitaer-schmidt.de",
            PasswordHash = string.Empty,
            Role = UserRole.Admin
        });

        builder.Entity<Project>().HasData(new Project
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000200"),
            TenantId = tenant2Id,
            Name = "Badsanierung Villa Berger",
            Customer = "Herr Berger",
            Address = "Bergstraße 5, 70173 Stuttgart",
            StartDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = ProjectStatus.Active,
            Description = "Komplettumbau Badezimmer OG"
        });
    }
}
