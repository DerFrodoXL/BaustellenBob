namespace BaustellenBob.Application.Interfaces;

public interface IApiKeyService
{
    Task<(string FullKey, Guid Id)> CreateKeyAsync(string name);
    Task<Guid?> ValidateKeyAsync(string key);
    Task<List<ApiKeyInfo>> GetAllAsync();
    Task RevokeAsync(Guid id);
}

public record ApiKeyInfo(Guid Id, string Name, string Prefix, DateTime CreatedAt, bool IsActive);
