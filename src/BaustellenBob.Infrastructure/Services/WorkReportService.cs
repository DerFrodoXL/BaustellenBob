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
        var reports = await _db.WorkReports
            .Where(w => w.ProjectId == projectId)
            .Include(w => w.User)
            .Include(w => w.Photos)
            .Include(w => w.MaterialEntries)
            .OrderByDescending(w => w.ReportDate)
            .ToListAsync();

        return reports.Select(w => new WorkReportDto
        {
            Id = w.Id,
            ProjectId = w.ProjectId,
            UserName = w.User.Name,
            ReportDate = w.ReportDate,
            Hours = w.Hours,
            Activity = w.Activity,
            Material = w.Material,
            CreatedAt = w.CreatedAt,
            PhotoCount = w.Photos.Count,
            MaterialEntries = w.MaterialEntries.Select(m => new MaterialEntryDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                WorkReportId = m.WorkReportId,
                Name = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
                UnitPrice = m.UnitPrice,
                IsInvoiced = m.IsInvoiced,
                CreatedAt = m.CreatedAt
            }).ToList()
        }).ToList();
    }

    public async Task<WorkReportDto> CreateAsync(Guid projectId, WorkReportDto dto)
    {
        await _tierLimits.EnsureCanCreateReportAsync();

        // Build a summary string from material entries for backward compat
        var materialSummary = dto.MaterialEntries.Count > 0
            ? string.Join(", ", dto.MaterialEntries.Select(m => $"{m.Quantity} {m.Unit} {m.Name}"))
            : dto.Material;

        var entity = new WorkReport
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            ProjectId = projectId,
            UserId = _currentUser.UserId,
            ReportDate = dto.ReportDate,
            Hours = dto.Hours,
            Activity = dto.Activity,
            Material = materialSummary,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkReports.Add(entity);

        // Create linked material entries
        foreach (var m in dto.MaterialEntries)
        {
            _db.MaterialEntries.Add(new BaustellenBob.Domain.Entities.MaterialEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.TenantId,
                ProjectId = projectId,
                WorkReportId = entity.Id,
                Name = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
                UnitPrice = m.UnitPrice,
                IsInvoiced = false,
                CreatedAt = DateTime.UtcNow
            });
        }

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

        // Replace material entries
        var existing = await _db.MaterialEntries
            .Where(m => m.WorkReportId == dto.Id)
            .ToListAsync();
        _db.MaterialEntries.RemoveRange(existing);

        var materialSummary = dto.MaterialEntries.Count > 0
            ? string.Join(", ", dto.MaterialEntries.Select(m => $"{m.Quantity} {m.Unit} {m.Name}"))
            : dto.Material;
        entity.Material = materialSummary;

        foreach (var m in dto.MaterialEntries)
        {
            _db.MaterialEntries.Add(new BaustellenBob.Domain.Entities.MaterialEntry
            {
                Id = Guid.NewGuid(),
                TenantId = entity.TenantId,
                ProjectId = entity.ProjectId,
                WorkReportId = entity.Id,
                Name = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
                UnitPrice = m.UnitPrice,
                IsInvoiced = false,
                CreatedAt = DateTime.UtcNow
            });
        }

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
