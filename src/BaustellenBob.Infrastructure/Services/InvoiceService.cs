using BaustellenBob.Application.DTOs;
using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Entities;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BaustellenBob.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public InvoiceService(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<InvoiceDto>> GetAllAsync()
    {
        return await _db.Set<Invoice>()
            .Include(i => i.Items)
            .Include(i => i.Project)
            .OrderByDescending(i => i.IssueDate)
            .Select(i => ToDto(i))
            .ToListAsync();
    }

    public async Task<List<InvoiceDto>> GetByProjectAsync(Guid projectId)
    {
        return await _db.Set<Invoice>()
            .Include(i => i.Items)
            .Include(i => i.Project)
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.IssueDate)
            .Select(i => ToDto(i))
            .ToListAsync();
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _db.Set<Invoice>()
            .Include(i => i.Items)
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == id);
        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<InvoiceDto> CreateFromProjectAsync(Guid projectId)
    {
        var project = await _db.Projects.FindAsync(projectId)
            ?? throw new InvalidOperationException("Project not found.");

        var materials = await _db.MaterialEntries
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();

        // Generate next invoice number for this tenant
        var count = await _db.Set<Invoice>().CountAsync() + 1;
        var invoiceNumber = $"RE-{DateTime.UtcNow:yyyy}-{count:D4}";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            ProjectId = projectId,
            InvoiceNumber = invoiceNumber,
            CustomerName = project.Customer,
            CustomerAddress = project.Address,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Draft,
            Notes = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Items = materials.Select(m => new InvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.TenantId,
                Description = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
                UnitPrice = m.UnitPrice
            }).ToList()
        };

        _db.Set<Invoice>().Add(invoice);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        return (await GetByIdAsync(invoice.Id))!;
    }

    public async Task UpdateAsync(InvoiceDto dto)
    {
        var entity = await _db.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == dto.Id)
            ?? throw new InvalidOperationException("Invoice not found.");

        entity.InvoiceNumber = dto.InvoiceNumber;
        entity.CustomerName = dto.CustomerName;
        entity.CustomerAddress = dto.CustomerAddress;
        entity.IssueDate = dto.IssueDate;
        entity.DueDate = dto.DueDate;
        entity.Status = dto.Status;
        entity.Notes = dto.Notes;

        // Replace all invoice items in a set-based way to avoid tracking concurrency conflicts.
        await _db.Set<InvoiceItem>()
            .Where(i => i.InvoiceId == entity.Id)
            .ExecuteDeleteAsync();

        var items = dto.Items.Select(i => new InvoiceItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            InvoiceId = entity.Id,
            Description = i.Description,
            Quantity = i.Quantity,
            Unit = i.Unit,
            UnitPrice = i.UnitPrice
        }).ToList();

        await _db.Set<InvoiceItem>().AddRangeAsync(items);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Set<Invoice>()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Invoice not found.");

        _db.Set<InvoiceItem>().RemoveRange(entity.Items);
        _db.Set<Invoice>().Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
    {
        var invoice = await _db.Set<Invoice>()
            .Include(i => i.Items)
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantProvider.TenantId);

        var companyName = tenant?.Name ?? "Firma";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(companyName).FontSize(18).Bold();
                        row.RelativeItem().AlignRight().Text("RECHNUNG").FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("An:").Bold();
                            left.Item().Text(invoice.CustomerName);
                            left.Item().Text(invoice.CustomerAddress);
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text($"Rechnungsnr.: {invoice.InvoiceNumber}").Bold();
                            right.Item().Text($"Datum: {invoice.IssueDate:dd.MM.yyyy}");
                            right.Item().Text($"Fällig: {invoice.DueDate:dd.MM.yyyy}");
                            if (invoice.Project != null)
                                right.Item().Text($"Baustelle: {invoice.Project.Name}");
                        });
                    });
                    col.Item().PaddingVertical(10).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("#").Bold();
                            header.Cell().Text("Beschreibung").Bold();
                            header.Cell().Text("Menge").Bold();
                            header.Cell().Text("Einheit").Bold();
                            header.Cell().Text("Einzelpreis").Bold();
                            header.Cell().AlignRight().Text("Gesamt").Bold();
                        });

                        var pos = 1;
                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Text(pos.ToString());
                            table.Cell().Text(item.Description);
                            table.Cell().Text(item.Quantity.ToString("N2"));
                            table.Cell().Text(item.Unit);
                            table.Cell().Text($"{item.UnitPrice:N2} €");
                            table.Cell().AlignRight().Text($"{(item.Quantity * item.UnitPrice):N2} €");
                            pos++;
                        }
                    });

                    var netto = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
                    var mwst = netto * 0.19m;
                    var brutto = netto + mwst;

                    col.Item().PaddingTop(15).AlignRight().Column(totals =>
                    {
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(120).Text("Netto:");
                            row.ConstantItem(100).AlignRight().Text($"{netto:N2} €");
                        });
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(120).Text("MwSt. (19%):");
                            row.ConstantItem(100).AlignRight().Text($"{mwst:N2} €");
                        });
                        totals.Item().PaddingTop(3).LineHorizontal(0.5f);
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(120).Text("Brutto:").Bold();
                            row.ConstantItem(100).AlignRight().Text($"{brutto:N2} €").Bold();
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        col.Item().PaddingTop(20).Text("Hinweise:").Bold();
                        col.Item().Text(invoice.Notes);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"{companyName} — Rechnung {invoice.InvoiceNumber} — Seite ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static InvoiceDto ToDto(Invoice i) => new()
    {
        Id = i.Id,
        ProjectId = i.ProjectId,
        ProjectName = i.Project?.Name ?? "",
        InvoiceNumber = i.InvoiceNumber,
        CustomerName = i.CustomerName,
        CustomerAddress = i.CustomerAddress,
        IssueDate = i.IssueDate,
        DueDate = i.DueDate,
        Status = i.Status,
        Notes = i.Notes,
        CreatedAt = i.CreatedAt,
        Items = i.Items.Select(item => new InvoiceItemDto
        {
            Id = item.Id,
            Description = item.Description,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice
        }).ToList()
    };
}
