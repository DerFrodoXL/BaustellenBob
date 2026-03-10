using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class TierLimitService : ITierLimitService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ITenantProvider _tenantProvider;

    public TierLimitService(IDbContextFactory<AppDbContext> dbFactory, ITenantProvider tenantProvider)
    {
        _dbFactory = dbFactory;
        _tenantProvider = tenantProvider;
    }

    public async Task<TierLimit> GetCurrentLimitsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await GetCurrentLimitsAsync(db);
    }

    private async Task<TierLimit> GetCurrentLimitsAsync(AppDbContext db)
    {
        var tenant = await db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");
        return TierLimits.For(tenant.Tier);
    }

    public async Task<TierUsage> GetUsageAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var activeProjects = await db.Projects.CountAsync(p =>
            p.Status == Domain.Enums.ProjectStatus.Active || p.Status == Domain.Enums.ProjectStatus.Paused);
        var employees = await db.Users.CountAsync();
        var photos = await db.Photos.CountAsync();
        var reports = await db.WorkReports.CountAsync();
        return new TierUsage(activeProjects, employees, photos, reports);
    }

    public async Task EnsureCanCreateProjectAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var limits = await GetCurrentLimitsAsync(db);
        var activeCount = await db.Projects.CountAsync(p =>
            p.Status == Domain.Enums.ProjectStatus.Active || p.Status == Domain.Enums.ProjectStatus.Paused);
        if (activeCount >= limits.MaxActiveProjects)
            throw new TierLimitExceededException("aktive Baustellen", limits.MaxActiveProjects);
    }

    public async Task EnsureCanCreateEmployeeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var limits = await GetCurrentLimitsAsync(db);
        var count = await db.Users.CountAsync();
        if (count >= limits.MaxEmployees)
            throw new TierLimitExceededException("Mitarbeiter", limits.MaxEmployees);
    }

    public async Task EnsureCanUploadPhotoAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var limits = await GetCurrentLimitsAsync(db);
        var count = await db.Photos.CountAsync();
        if (count >= limits.MaxPhotos)
            throw new TierLimitExceededException("Fotos", limits.MaxPhotos);
    }

    public async Task EnsureCanCreateReportAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var limits = await GetCurrentLimitsAsync(db);
        var count = await db.WorkReports.CountAsync();
        if (count >= limits.MaxReports)
            throw new TierLimitExceededException("Rapporte", limits.MaxReports);
    }

    public async Task<bool> CanExportPdfAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var limits = await GetCurrentLimitsAsync(db);
        return limits.PdfExport;
    }

    public async Task<string> GetCurrentTierNameAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");
        return tenant.Tier.ToString();
    }
}
