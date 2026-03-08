namespace BaustellenBob.Domain.Entities;

public class ProjectAssignment : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
