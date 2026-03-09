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
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null
                    ? (p.Customer.Company != null ? $"{p.Customer.Name} ({p.Customer.Company})" : p.Customer.Name)
                    : string.Empty,
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

        // Timeline: active projects with today's assigned workers
        var today = DateTime.Today;
        var activeProjects = await _db.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderBy(p => p.Name)
            .Select(p => new TimelineProjectDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                Customer = p.Customer != null
                    ? (p.Customer.Company != null ? $"{p.Customer.Name} ({p.Customer.Company})" : p.Customer.Name)
                    : string.Empty,
                Address = p.Address
            })
            .ToListAsync();

        var projectIds = activeProjects.Select(p => p.ProjectId).ToList();
        var assignments = await _db.ProjectAssignments
            .Where(a => projectIds.Contains(a.ProjectId) && a.StartDate <= today && a.EndDate >= today)
            .Select(a => new
            {
                a.ProjectId,
                Worker = new TimelineWorkerDto
                {
                    UserId = a.UserId,
                    Name = a.User.Name,
                    ProfilePicturePath = a.User.ProfilePicturePath,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Notes = a.Notes
                }
            })
            .ToListAsync();

        var workersByProject = assignments.GroupBy(a => a.ProjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Worker).ToList());

        foreach (var p in activeProjects)
        {
            if (workersByProject.TryGetValue(p.ProjectId, out var workers))
                p.Workers = workers;
        }

        dto.TimelineProjects = activeProjects;

        return dto;
    }
}
