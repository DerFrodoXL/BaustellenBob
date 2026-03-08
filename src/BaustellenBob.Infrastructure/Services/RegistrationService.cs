using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class RegistrationService : IRegistrationService
{
    private readonly AppDbContext _db;

    public RegistrationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RegistrationResult> RegisterAsync(string companyName, string adminName, string email, string password)
    {
        // Check if email already exists (across all tenants)
        var exists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email);

        if (exists)
            return new RegistrationResult { Success = false, Error = "Diese E-Mail-Adresse ist bereits registriert." };

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = companyName,
            CreatedAt = DateTime.UtcNow,
            Tier = Tier.Free
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = adminName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new RegistrationResult
        {
            Success = true,
            TenantId = tenant.Id,
            UserId = user.Id
        };
    }
}
