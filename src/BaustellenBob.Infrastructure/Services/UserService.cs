using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Infrastructure.Storage;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITierLimitService _tierLimits;

    public UserService(AppDbContext db, ITenantProvider tenantProvider, ITierLimitService tierLimits)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _tierLimits = tierLimits;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        return await _db.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                ProfilePicturePath = u.ProfilePicturePath
            })
            .ToListAsync();
    }

    public async Task<UserDto> CreateAsync(UserDto dto)
    {
        await _tierLimits.EnsureCanCreateEmployeeAsync();
        var entity = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Name = dto.Name,
            Email = dto.Email,
            Role = dto.Role,
            PasswordHash = string.IsNullOrWhiteSpace(dto.NewPassword)
                ? string.Empty
                : BCrypt.Net.BCrypt.HashPassword(dto.NewPassword)
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.NewPassword = null;
        return dto;
    }

    public async Task UpdateAsync(UserDto dto)
    {
        var entity = await _db.Users.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Benutzer nicht gefunden.");
        entity.Name = dto.Name;
        entity.Email = dto.Email;
        entity.Role = dto.Role;
        if (dto.ProfilePicturePath is not null)
            entity.ProfilePicturePath = dto.ProfilePicturePath;

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Users.FindAsync(id)
            ?? throw new InvalidOperationException("Benutzer nicht gefunden.");
        _db.Users.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<string> UploadProfilePictureAsync(Guid userId, string fileName, Stream stream)
    {
        var tenantId = _tenantProvider.TenantId;
        // Always save as JPEG – max 256x256 px, ~Q82 ~= 10-25 KB
        var safeFileName = $"{userId}.jpg";
        var bytes = await ImageProcessor.ToJpegBytesAsync(stream, maxWidth: 256, maxHeight: 256, quality: 82);

        var relativePath = $"{tenantId}/avatars/{safeFileName}";

        var entity = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Benutzer nicht gefunden.");
        entity.ProfilePicturePath = relativePath;
        entity.ProfilePictureData = bytes;
        entity.ProfilePictureContentType = "image/jpeg";
        await _db.SaveChangesAsync();

        return relativePath;
    }

    public async Task<string?> GetProfilePicturePathAsync(Guid userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.ProfilePicturePath)
            .FirstOrDefaultAsync();
    }
}
