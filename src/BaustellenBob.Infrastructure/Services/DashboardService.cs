using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var dto = new DashboardDto();

        // Single aggregate queries instead of N+1
        dto.ActiveProjects = await _db.Projects.CountAsync(p => p.Status == ProjectStatus.Active);
        dto.TotalPhotos = await _db.Photos.CountAsync();
        dto.TotalReports = await _db.WorkReports.CountAsync();
        dto.TotalHours = await _db.WorkReports.SumAsync(w => (decimal?)w.Hours) ?? 0;

        dto.RecentProjects = await _db.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderByDescending(p => p.StartDate)
            .Take(6)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Customer = p.Customer,
                Address = p.Address,
                StartDate = p.StartDate,
                Status = p.Status
            })
            .ToListAsync();

        // Recent activities: last 10 photos + reports combined
        var recentPhotos = await _db.Photos
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivityDto
            {
                Icon = "PhotoCamera",
                Text = $"{p.UploadedBy.Name} hat ein Foto hochgeladen",
                Timestamp = p.CreatedAt,
                ProjectId = p.ProjectId,
                ProjectName = p.Project.Name
            })
            .ToListAsync();

        var recentReports = await _db.WorkReports
            .OrderByDescending(w => w.CreatedAt)
            .Take(5)
            .Select(w => new RecentActivityDto
            {
                Icon = "Assignment",
                Text = $"{w.User.Name}: {w.Hours}h — {w.Activity}",
                Timestamp = w.CreatedAt,
                ProjectId = w.ProjectId,
                ProjectName = w.Project.Name
            })
            .ToListAsync();

        dto.RecentActivities = recentPhotos
            .Concat(recentReports)
            .OrderByDescending(a => a.Timestamp)
            .Take(8)
            .ToList();

        return dto;
    }
}
