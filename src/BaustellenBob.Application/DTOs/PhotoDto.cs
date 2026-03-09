namespace BaustellenBob.Application.DTOs;

public class PhotoDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? WorkReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
