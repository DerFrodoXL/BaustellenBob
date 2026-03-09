namespace BaustellenBob.Application.DTOs;

public class MaterialEntryDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? WorkReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsInvoiced { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public DateTime CreatedAt { get; set; }
}
