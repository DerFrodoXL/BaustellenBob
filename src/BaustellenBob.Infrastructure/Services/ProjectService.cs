using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;
    private readonly Shared.Interfaces.ITenantProvider _tenantProvider;
    private readonly ITierLimitService _tierLimits;

    public ProjectService(AppDbContext db, Shared.Interfaces.ITenantProvider tenantProvider, ITierLimitService tierLimits)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _tierLimits = tierLimits;
    }

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        return await _db.Projects
            .OrderByDescending(b => b.StartDate)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var b = await _db.Projects.FindAsync(id);
        return b is null ? null : ToDto(b);
    }

    public async Task<ProjectDto> CreateAsync(ProjectDto dto)
    {
        await _tierLimits.EnsureCanCreateProjectAsync();
        var entity = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Name = dto.Name,
            Customer = dto.Customer,
            Address = dto.Address,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = ProjectStatus.Active,
            Description = dto.Description
        };
        _db.Projects.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateAsync(ProjectDto dto)
    {
        var entity = await _db.Projects.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Project not found.");
        entity.Name = dto.Name;
        entity.Customer = dto.Customer;
        entity.Address = dto.Address;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Status = dto.Status;
        entity.Description = dto.Description;
        await _db.SaveChangesAsync();
    }

    public async Task ArchiveAsync(Guid id)
    {
        var entity = await _db.Projects.FindAsync(id)
            ?? throw new InvalidOperationException("Project not found.");
        entity.Status = ProjectStatus.Archived;
        await _db.SaveChangesAsync();
    }

    private static ProjectDto ToDto(Project b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Customer = b.Customer,
        Address = b.Address,
        StartDate = b.StartDate,
        EndDate = b.EndDate,
        Status = b.Status,
        Description = b.Description
    };
}
