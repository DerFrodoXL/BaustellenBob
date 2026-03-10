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
    private readonly Shared.Interfaces.ICurrentUserProvider _currentUser;
    private readonly ITierLimitService _tierLimits;

    public ProjectService(AppDbContext db, Shared.Interfaces.ITenantProvider tenantProvider, Shared.Interfaces.ICurrentUserProvider currentUser, ITierLimitService tierLimits)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _tierLimits = tierLimits;
    }

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        return await _db.Projects
            .Include(b => b.Customer)
            .OrderByDescending(b => b.StartDate)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var b = await _db.Projects.Include(b => b.Customer).FirstOrDefaultAsync(b => b.Id == id);
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
            CustomerId = dto.CustomerId,
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
        entity.CustomerId = dto.CustomerId;
        entity.Address = dto.Address;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Status = dto.Status;
        entity.Description = dto.Description;
        await _db.SaveChangesAsync();
    }

    public async Task ArchiveAsync(Guid id)
    {
        if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Nur Admins dürfen Baustellen archivieren.");

        var entity = await _db.Projects.FindAsync(id)
            ?? throw new InvalidOperationException("Project not found.");
        entity.Status = ProjectStatus.Archived;
        await _db.SaveChangesAsync();
    }

    private static ProjectDto ToDto(Project b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer?.Company is not null
            ? $"{b.Customer.Name} ({b.Customer.Company})"
            : b.Customer?.Name ?? string.Empty,
        Address = b.Address,
        StartDate = b.StartDate,
        EndDate = b.EndDate,
        Status = b.Status,
        Description = b.Description
    };
}
