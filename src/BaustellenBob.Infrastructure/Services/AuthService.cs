using BaustellenBob.Application.Interfaces;
using BaustellenBob.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        // IgnoreQueryFilters: login must work across tenants to find the user
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return null;

        // Empty hash = demo user with any password (dev only), otherwise verify BCrypt
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
        }

        return new AuthResult
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            UserName = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            TenantName = user.Tenant.Name
        };
    }
}
