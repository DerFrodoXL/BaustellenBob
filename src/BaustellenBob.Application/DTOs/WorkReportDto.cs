namespace BaustellenBob.Application.DTOs;

public class WorkReportDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public decimal Hours { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PhotoCount { get; set; }
    public List<MaterialEntryDto> MaterialEntries { get; set; } = new();
}
