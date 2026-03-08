using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetAllAsync();
    Task<List<InvoiceDto>> GetByProjectAsync(Guid projectId);
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceDto> CreateFromProjectAsync(Guid projectId);
    Task UpdateAsync(InvoiceDto dto);
    Task DeleteAsync(Guid id);
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);
}
