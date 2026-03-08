namespace BaustellenBob.Domain.Entities;

public class MaterialEntry : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
