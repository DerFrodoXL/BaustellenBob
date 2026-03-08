using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class TierLimitService : ITierLimitService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public TierLimitService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<TierLimit> GetCurrentLimitsAsync()
    {
        var tenant = await _db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");
        return TierLimits.For(tenant.Tier);
    }

    public async Task<TierUsage> GetUsageAsync()
    {
        var tenantId = _tenantProvider.TenantId;
        var projects = await _db.Projects.CountAsync();
        var employees = await _db.Users.CountAsync();
        var photos = await _db.Photos.CountAsync();
        var reports = await _db.WorkReports.CountAsync();
        return new TierUsage(projects, employees, photos, reports);
    }

    public async Task EnsureCanCreateProjectAsync()
    {
        var limits = await GetCurrentLimitsAsync();
        var count = await _db.Projects.CountAsync();
        if (count >= limits.MaxProjects)
            throw new TierLimitExceededException("Baustellen", limits.MaxProjects);
    }

    public async Task EnsureCanCreateEmployeeAsync()
    {
        var limits = await GetCurrentLimitsAsync();
        var count = await _db.Users.CountAsync();
        if (count >= limits.MaxEmployees)
            throw new TierLimitExceededException("Mitarbeiter", limits.MaxEmployees);
    }

    public async Task EnsureCanUploadPhotoAsync()
    {
        var limits = await GetCurrentLimitsAsync();
        var count = await _db.Photos.CountAsync();
        if (count >= limits.MaxPhotos)
            throw new TierLimitExceededException("Fotos", limits.MaxPhotos);
    }

    public async Task EnsureCanCreateReportAsync()
    {
        var limits = await GetCurrentLimitsAsync();
        var count = await _db.WorkReports.CountAsync();
        if (count >= limits.MaxReports)
            throw new TierLimitExceededException("Rapporte", limits.MaxReports);
    }

    public async Task<bool> CanExportPdfAsync()
    {
        var limits = await GetCurrentLimitsAsync();
        return limits.PdfExport;
    }
}
