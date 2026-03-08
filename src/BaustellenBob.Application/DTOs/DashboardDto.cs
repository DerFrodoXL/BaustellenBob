namespace BaustellenBob.Application.DTOs;

public class DashboardDto
{
    public int ActiveProjects { get; set; }
    public int TotalPhotos { get; set; }
    public int TotalReports { get; set; }
    public decimal TotalHours { get; set; }
    public List<ProjectDto> RecentProjects { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class RecentActivityDto
{
    public string Icon { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
