namespace BaustellenBob.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
