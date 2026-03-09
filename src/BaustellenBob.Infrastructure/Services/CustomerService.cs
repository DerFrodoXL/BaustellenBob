using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BaustellenBob.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public CustomerService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await _db.Customers
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id);
        return c is null ? null : ToDto(c);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto dto)
    {
        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Name = dto.Name,
            Company = dto.Company,
            Phone = dto.Phone,
            Email = dto.Email,
            Street = dto.Street,
            Zip = dto.Zip,
            City = dto.City,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        dto.CreatedAt = entity.CreatedAt;
        return dto;
    }

    public async Task UpdateAsync(CustomerDto dto)
    {
        var entity = await _db.Customers.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("Customer not found.");
        entity.Name = dto.Name;
        entity.Company = dto.Company;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Street = dto.Street;
        entity.Zip = dto.Zip;
        entity.City = dto.City;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException("Customer not found.");
        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private static CustomerDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Company = c.Company,
        Phone = c.Phone,
        Email = c.Email,
        Street = c.Street,
        Zip = c.Zip,
        City = c.City,
        CreatedAt = c.CreatedAt
    };
}
