namespace BaustellenBob.Domain.Entities;

public class Photo : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? WorkReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public byte[]? FileData { get; set; }
    public string? FileContentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Project Project { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
    public WorkReport? WorkReport { get; set; }
}
