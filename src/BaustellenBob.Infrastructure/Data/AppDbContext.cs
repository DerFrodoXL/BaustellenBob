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
    public DbSet<MaterialEntry> MaterialEntries => Set<MaterialEntry>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

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

        // MaterialEntry
        builder.Entity<MaterialEntry>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(300).IsRequired();
            e.Property(m => m.Unit).HasMaxLength(50);
            e.Property(m => m.Quantity).HasPrecision(10, 2);
            e.Property(m => m.UnitPrice).HasPrecision(10, 2);
            e.HasOne(m => m.Project).WithMany(b => b.Materials).HasForeignKey(m => m.ProjectId);
            e.HasQueryFilter(m => m.TenantId == _tenantProvider.TenantId);
        });

        // ProjectAssignment
        builder.Entity<ProjectAssignment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Notes).HasMaxLength(500);
            e.HasOne(a => a.Project).WithMany(b => b.Assignments).HasForeignKey(a => a.ProjectId);
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
            e.HasQueryFilter(a => a.TenantId == _tenantProvider.TenantId);
        });

        // Invoice
        builder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(i => i.CustomerName).HasMaxLength(200);
            e.Property(i => i.CustomerAddress).HasMaxLength(500);
            e.Property(i => i.Notes).HasMaxLength(2000);
            e.HasOne(i => i.Project).WithMany(p => p.Invoices).HasForeignKey(i => i.ProjectId);
            e.HasQueryFilter(i => i.TenantId == _tenantProvider.TenantId);
        });

        // InvoiceItem
        builder.Entity<InvoiceItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Description).HasMaxLength(500).IsRequired();
            e.Property(i => i.Unit).HasMaxLength(50);
            e.Property(i => i.Quantity).HasPrecision(10, 2);
            e.Property(i => i.UnitPrice).HasPrecision(10, 2);
            e.HasOne(i => i.Invoice).WithMany(inv => inv.Items).HasForeignKey(i => i.InvoiceId);
            e.HasQueryFilter(i => i.TenantId == _tenantProvider.TenantId);
        });

        // ApiKey (not tenant-filtered via query filter — validated differently)
        builder.Entity<ApiKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Name).HasMaxLength(200).IsRequired();
            e.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
            e.Property(k => k.KeyPrefix).HasMaxLength(20).IsRequired();
            e.HasOne(k => k.Tenant).WithMany().HasForeignKey(k => k.TenantId);
            e.HasIndex(k => k.KeyHash);
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
