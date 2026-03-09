using BaustellenBob.Application.Interfaces;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Infrastructure.Storage;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public TenantService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<string?> GetLogoPathAsync()
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenantProvider.TenantId);
        return tenant?.LogoPath;
    }

    public async Task<string> UploadLogoAsync(string fileName, Stream stream)
    {
        var tenantId = _tenantProvider.TenantId;
        // Always save as PNG to preserve transparency - max 400x150 px
        var safeFileName = "logo.png";
        var bytes = await ImageProcessor.ToPngBytesAsync(stream, maxWidth: 400, maxHeight: 150);

        var relativePath = $"{tenantId}/logo/{safeFileName}";

        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");
        tenant.LogoPath = relativePath;
        tenant.LogoData = bytes;
        tenant.LogoContentType = "image/png";
        await _db.SaveChangesAsync();

        return relativePath;
    }
}
