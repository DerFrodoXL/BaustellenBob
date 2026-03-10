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
    public DbSet<Customer> Customers => Set<Customer>();
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
            e.Property(t => t.LogoContentType).HasMaxLength(100);
            e.Property(t => t.StripeCustomerId).HasMaxLength(100);
            e.Property(t => t.StripeSubscriptionId).HasMaxLength(100);
            e.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired().HasDefaultValue("EUR");
        });

        // User
        builder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.Property(u => u.Name).HasMaxLength(200).IsRequired();
            e.Property(u => u.ProfilePictureContentType).HasMaxLength(100);
            e.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId);
            e.HasQueryFilter(u => u.TenantId == _tenantProvider.TenantId);
        });

        // Customer
        builder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Company).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Street).HasMaxLength(300);
            e.Property(c => c.Zip).HasMaxLength(20);
            e.Property(c => c.City).HasMaxLength(100);
            e.HasOne(c => c.Tenant).WithMany(t => t.Customers).HasForeignKey(c => c.TenantId);
            e.HasQueryFilter(c => c.TenantId == _tenantProvider.TenantId);
        });

        // Project
        builder.Entity<Project>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
            e.Property(b => b.Address).HasMaxLength(500);
            e.HasOne(b => b.Tenant).WithMany(t => t.Projects).HasForeignKey(b => b.TenantId);
            e.HasOne(b => b.Customer).WithMany(c => c.Projects).HasForeignKey(b => b.CustomerId).IsRequired(false);
            e.HasQueryFilter(b => b.TenantId == _tenantProvider.TenantId);
        });

        // Photo
        builder.Entity<Photo>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FilePath).HasMaxLength(500).IsRequired();
            e.Property(p => p.FileContentType).HasMaxLength(100);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.HasOne(p => p.Project).WithMany(b => b.Photos).HasForeignKey(p => p.ProjectId);
            e.HasOne(p => p.UploadedBy).WithMany().HasForeignKey(p => p.UploadedByUserId);
            e.HasOne(p => p.WorkReport).WithMany(w => w.Photos).HasForeignKey(p => p.WorkReportId).IsRequired(false);
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
            e.HasOne(m => m.WorkReport).WithMany(w => w.MaterialEntries).HasForeignKey(m => m.WorkReportId).IsRequired(false);
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

    
     

     
    }
}
