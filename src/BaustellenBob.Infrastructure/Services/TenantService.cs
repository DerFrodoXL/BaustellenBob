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
    private readonly string _uploadRoot;

    public TenantService(AppDbContext db, ITenantProvider tenantProvider, string uploadRoot = "uploads")
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _uploadRoot = uploadRoot;
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
        var dir = Path.Combine(_uploadRoot, tenantId.ToString(), "logo");
        Directory.CreateDirectory(dir);

        // Always save as PNG to preserve transparency – max 400×150 px
        var safeFileName = "logo.png";
        var fullPath = Path.Combine(dir, safeFileName);
        var normalizedFull = Path.GetFullPath(fullPath);
        var normalizedRoot = Path.GetFullPath(_uploadRoot);
        if (!normalizedFull.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ungültiger Dateipfad.");

        await ImageProcessor.SaveAsPngAsync(stream, fullPath, maxWidth: 400, maxHeight: 150);

        var relativePath = $"{tenantId}/logo/{safeFileName}";

        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");
        tenant.LogoPath = relativePath;
        await _db.SaveChangesAsync();

        return relativePath;
    }
}
