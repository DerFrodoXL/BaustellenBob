namespace BaustellenBob.Application.DTOs;

public class DashboardDto
{
    public int ActiveProjects { get; set; }
    public int TotalPhotos { get; set; }
    public int TotalReports { get; set; }
    public decimal TotalHours { get; set; }
    public List<ProjectDto> RecentProjects { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<TimelineProjectDto> TimelineProjects { get; set; } = new();
}

public class RecentActivityDto
{
    public string Icon { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class TimelineProjectDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<TimelineWorkerDto> Workers { get; set; } = new();
}

public class TimelineWorkerDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePicturePath { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
