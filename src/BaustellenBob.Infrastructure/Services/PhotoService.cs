using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class PhotoService : IPhotoService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ITierLimitService _tierLimits;
    private readonly string _uploadRoot;

    public PhotoService(AppDbContext db, ITenantProvider tenantProvider, ICurrentUserProvider currentUser, ITierLimitService tierLimits, string uploadRoot = "uploads")
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _tierLimits = tierLimits;
        _uploadRoot = uploadRoot;
    }

    public async Task<List<PhotoDto>> GetByProjectAsync(Guid projectId)
    {
        return await _db.Photos
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                FilePath = p.FilePath,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UploadedByName = p.UploadedBy.Name,
                Latitude = p.Latitude,
                Longitude = p.Longitude
            })
            .ToListAsync();
    }

    public async Task<PhotoDto> UploadAsync(Guid projectId, string fileName, Stream fileStream, string description, double? latitude, double? longitude)
    {
        await _tierLimits.EnsureCanUploadPhotoAsync();
        var tenantId = _tenantProvider.TenantId;
        var dir = Path.Combine(_uploadRoot, tenantId.ToString(), projectId.ToString());
        Directory.CreateDirectory(dir);

        var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(dir, safeFileName);

        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }

        var relativePath = $"{tenantId}/{projectId}/{safeFileName}";

        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            UploadedByUserId = _currentUser.UserId,
            FilePath = relativePath,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude
        };
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();

        return new PhotoDto
        {
            Id = photo.Id,
            ProjectId = photo.ProjectId,
            FilePath = photo.FilePath,
            Description = photo.Description,
            CreatedAt = photo.CreatedAt,
            UploadedByName = _currentUser.UserName,
            Latitude = photo.Latitude,
            Longitude = photo.Longitude
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var photo = await _db.Photos.FindAsync(id)
            ?? throw new InvalidOperationException("Photo not found.");

        var fullPath = Path.Combine(_uploadRoot, photo.FilePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();
    }
}
