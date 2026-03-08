using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class WorkReportService : IWorkReportService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ITierLimitService _tierLimits;

    public WorkReportService(AppDbContext db, ITenantProvider tenantProvider, ICurrentUserProvider currentUser, ITierLimitService tierLimits)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _tierLimits = tierLimits;
    }

    public async Task<List<WorkReportDto>> GetByProjectAsync(Guid projectId)
    {
        return await _db.WorkReports
            .Where(w => w.ProjectId == projectId)
            .OrderByDescending(w => w.ReportDate)
            .Select(w => new WorkReportDto
            {
                Id = w.Id,
                ProjectId = w.ProjectId,
                UserName = w.User.Name,
                ReportDate = w.ReportDate,
                Hours = w.Hours,
                Activity = w.Activity,
                Material = w.Material,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<WorkReportDto> CreateAsync(Guid projectId, WorkReportDto dto)
    {
        await _tierLimits.EnsureCanCreateReportAsync();
        var entity = new WorkReport
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            ProjectId = projectId,
            UserId = _currentUser.UserId,
            ReportDate = dto.ReportDate,
            Hours = dto.Hours,
            Activity = dto.Activity,
            Material = dto.Material,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkReports.Add(entity);
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.UserName = _currentUser.UserName;
        dto.CreatedAt = entity.CreatedAt;
        return dto;
    }

    public async Task UpdateAsync(WorkReportDto dto)
    {
        var entity = await _db.WorkReports.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Work report not found.");
        entity.ReportDate = dto.ReportDate;
        entity.Hours = dto.Hours;
        entity.Activity = dto.Activity;
        entity.Material = dto.Material;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.WorkReports.FindAsync(id)
            ?? throw new InvalidOperationException("Work report not found.");
        _db.WorkReports.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
