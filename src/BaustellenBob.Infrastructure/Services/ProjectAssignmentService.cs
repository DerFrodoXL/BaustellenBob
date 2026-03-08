using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public ProjectAssignmentService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<ProjectAssignmentDto>> GetByProjectAsync(Guid projectId)
    {
        return await _db.ProjectAssignments
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.StartDate)
            .Select(a => new ProjectAssignmentDto
            {
                Id = a.Id,
                ProjectId = a.ProjectId,
                ProjectName = a.Project.Name,
                UserId = a.UserId,
                UserName = a.User.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Notes = a.Notes
            })
            .ToListAsync();
    }

    public async Task<List<ProjectAssignmentDto>> GetByWeekAsync(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        return await _db.ProjectAssignments
            .Where(a => a.StartDate < weekEnd && a.EndDate >= weekStart)
            .OrderBy(a => a.StartDate)
            .Select(a => new ProjectAssignmentDto
            {
                Id = a.Id,
                ProjectId = a.ProjectId,
                ProjectName = a.Project.Name,
                UserId = a.UserId,
                UserName = a.User.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Notes = a.Notes
            })
            .ToListAsync();
    }

    public async Task<ProjectAssignmentDto> CreateAsync(ProjectAssignmentDto dto)
    {
        var entity = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            ProjectId = dto.ProjectId,
            UserId = dto.UserId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Notes = dto.Notes
        };
        _db.ProjectAssignments.Add(entity);
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateAsync(ProjectAssignmentDto dto)
    {
        var entity = await _db.ProjectAssignments.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Zuordnung nicht gefunden.");
        entity.ProjectId = dto.ProjectId;
        entity.UserId = dto.UserId;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Notes = dto.Notes;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.ProjectAssignments.FindAsync(id)
            ?? throw new InvalidOperationException("Zuordnung nicht gefunden.");
        _db.ProjectAssignments.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
