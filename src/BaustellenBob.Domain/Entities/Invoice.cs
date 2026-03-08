using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
