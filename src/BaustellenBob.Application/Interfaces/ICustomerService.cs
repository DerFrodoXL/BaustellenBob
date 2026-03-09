using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<CustomerDto> CreateAsync(CustomerDto dto);
    Task UpdateAsync(CustomerDto dto);
    Task DeleteAsync(Guid id);
}
