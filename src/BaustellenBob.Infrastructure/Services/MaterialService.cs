using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class MaterialService : IMaterialService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public MaterialService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<MaterialEntryDto>> GetByProjectAsync(Guid projectId)
    {
        return await _db.MaterialEntries
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaterialEntryDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                Name = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
                UnitPrice = m.UnitPrice,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MaterialEntryDto> CreateAsync(Guid projectId, MaterialEntryDto dto)
    {
        var entity = new MaterialEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            ProjectId = projectId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            UnitPrice = dto.UnitPrice,
            CreatedAt = DateTime.UtcNow
        };
        _db.MaterialEntries.Add(entity);
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.CreatedAt = entity.CreatedAt;
        return dto;
    }

    public async Task UpdateAsync(MaterialEntryDto dto)
    {
        var entity = await _db.MaterialEntries.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Material nicht gefunden.");
        entity.Name = dto.Name;
        entity.Quantity = dto.Quantity;
        entity.Unit = dto.Unit;
        entity.UnitPrice = dto.UnitPrice;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.MaterialEntries.FindAsync(id)
            ?? throw new InvalidOperationException("Material nicht gefunden.");
        _db.MaterialEntries.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
