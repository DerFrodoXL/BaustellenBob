using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Infrastructure.Storage;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class TenantService : ITenantService
{
    private static readonly DayOfWeek[] DefaultWorkingDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ITenantProvider _tenantProvider;

    public TenantService(IDbContextFactory<AppDbContext> dbFactory, ITenantProvider tenantProvider)
    {
        _dbFactory = dbFactory;
        _tenantProvider = tenantProvider;
    }

    public async Task<string?> GetLogoPathAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenantProvider.TenantId);
        return tenant?.LogoPath;
    }

    public async Task<string> GetCurrencyCodeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenantProvider.TenantId);

        return NormalizeCurrencyCode(tenant?.CurrencyCode);
    }

    public async Task SetCurrencyCodeAsync(string currencyCode)
    {
        var normalized = NormalizeCurrencyCode(currencyCode);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");

        tenant.CurrencyCode = normalized;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<DayOfWeek>> GetWorkingDaysAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenantProvider.TenantId);

        return ParseWorkingDays(tenant?.WorkingDays);
    }

    public async Task SetWorkingDaysAsync(IEnumerable<DayOfWeek> workingDays)
    {
        var normalized = SerializeWorkingDays(workingDays);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");

        tenant.WorkingDays = normalized;
        await db.SaveChangesAsync();
    }

    public async Task<string> UploadLogoAsync(string fileName, Stream stream)
    {
        var tenantId = _tenantProvider.TenantId;
        // Always save as PNG to preserve transparency - max 400x150 px
        var safeFileName = "logo.png";
        var bytes = await ImageProcessor.ToPngBytesAsync(stream, maxWidth: 400, maxHeight: 150);

        var relativePath = $"{tenantId}/logo/{safeFileName}";

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");
        tenant.LogoPath = relativePath;
        tenant.LogoData = bytes;
        tenant.LogoContentType = "image/png";
        await db.SaveChangesAsync();

        return relativePath;
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
    {
        var code = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            return "EUR";

        return code.Length > 3 ? code[..3] : code;
    }

    private static IReadOnlyCollection<DayOfWeek> ParseWorkingDays(string? serializedWorkingDays)
    {
        var days = (serializedWorkingDays ?? Tenant.DefaultWorkingDays)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : (int?)null)
            .Where(value => value.HasValue && Enum.IsDefined(typeof(DayOfWeek), value.Value))
            .Select(value => (DayOfWeek)value!.Value)
            .Distinct()
            .OrderBy(day => SortDay(day))
            .ToArray();

        return days.Length > 0 ? days : DefaultWorkingDays;
    }

    private static string SerializeWorkingDays(IEnumerable<DayOfWeek> workingDays)
    {
        var normalized = workingDays
            .Distinct()
            .OrderBy(day => SortDay(day))
            .ToArray();

        if (normalized.Length == 0)
            throw new InvalidOperationException("Mindestens ein Arbeitstag muss ausgewählt sein.");

        return string.Join(',', normalized.Select(day => (int)day));
    }

    private static int SortDay(DayOfWeek day) => day == DayOfWeek.Sunday ? 7 : (int)day;
}
