using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns user info on success.
    /// </summary>
    Task<AuthResult?> LoginAsync(string email, string password);
}

public class AuthResult
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
