namespace BaustellenBob.Domain.Entities;

public class MaterialEntry : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? WorkReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsInvoiced { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public WorkReport? WorkReport { get; set; }
}
