using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;

    public Customer? Customer { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<WorkReport> WorkReports { get; set; } = new List<WorkReport>();
    public ICollection<MaterialEntry> Materials { get; set; } = new List<MaterialEntry>();
    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
