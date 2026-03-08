using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BaustellenBob.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public ApiKeyService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<(string FullKey, Guid Id)> CreateKeyAsync(string name)
    {
        var rawKey = GenerateKey();
        var prefix = rawKey[..8];
        var hash = HashKey(rawKey);

        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Name = name,
            KeyHash = hash,
            KeyPrefix = prefix,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Set<ApiKey>().Add(entity);
        await _db.SaveChangesAsync();

        return ($"bb_{rawKey}", entity.Id);
    }

    public async Task<Guid?> ValidateKeyAsync(string key)
    {
        if (!key.StartsWith("bb_") || key.Length < 11)
            return null;

        var rawKey = key[3..];
        var hash = HashKey(rawKey);

        var apiKey = await _db.Set<ApiKey>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

        return apiKey?.TenantId;
    }

    public async Task<List<ApiKeyInfo>> GetAllAsync()
    {
        return await _db.Set<ApiKey>()
            .Where(k => k.TenantId == _tenantProvider.TenantId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyInfo(k.Id, k.Name, k.KeyPrefix, k.CreatedAt, k.IsActive))
            .ToListAsync();
    }

    public async Task RevokeAsync(Guid id)
    {
        var entity = await _db.Set<ApiKey>()
            .Where(k => k.TenantId == _tenantProvider.TenantId && k.Id == id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("API key not found.");

        entity.IsActive = false;
        await _db.SaveChangesAsync();
    }

    private static string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
    }

    private static string HashKey(string key)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
