using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Application.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
}
