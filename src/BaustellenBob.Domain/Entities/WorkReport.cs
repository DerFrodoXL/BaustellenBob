namespace BaustellenBob.Domain.Entities;

public class WorkReport : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReportDate { get; set; }
    public decimal Hours { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<MaterialEntry> MaterialEntries { get; set; } = new List<MaterialEntry>();
}
