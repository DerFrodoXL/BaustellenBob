using BaustellenBob.Application.Interfaces;
using BaustellenBob.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BaustellenBob.Infrastructure.Services;

public class ProjectReportService : IProjectReportService
{
    private readonly AppDbContext _db;
    private readonly string _uploadRoot;

    public ProjectReportService(AppDbContext db, string uploadRoot)
    {
        _db = db;
        _uploadRoot = uploadRoot;
    }

    public async Task<byte[]> GenerateReportAsync(Guid projectId)
    {
        var project = await _db.Projects
            .FirstOrDefaultAsync(b => b.Id == projectId)
            ?? throw new InvalidOperationException("Project not found.");

        var photos = await _db.Photos
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.UploadedBy)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var reports = await _db.WorkReports
            .Where(w => w.ProjectId == projectId)
            .Include(w => w.User)
            .OrderBy(w => w.ReportDate)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);

                page.Header().Column(col =>
                {
                    col.Item().Text("Baustellenbericht").FontSize(22).Bold();
                    col.Item().PaddingBottom(10).Text(project.Name).FontSize(16).SemiBold();
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Kunde: {project.Customer}");
                        row.RelativeItem().Text($"Adresse: {project.Address}");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Start: {project.StartDate:dd.MM.yyyy}");
                        row.RelativeItem().Text($"Status: {project.Status}");
                    });
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    if (reports.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Arbeitsberichte").FontSize(14).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(1.5f);
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Datum").Bold();
                                header.Cell().Text("Mitarbeiter").Bold();
                                header.Cell().Text("Stunden").Bold();
                                header.Cell().Text("Tätigkeit").Bold();
                                header.Cell().Text("Material").Bold();
                            });

                            foreach (var r in reports)
                            {
                                table.Cell().Text(r.ReportDate.ToString("dd.MM.yyyy"));
                                table.Cell().Text(r.User?.Name ?? "-");
                                table.Cell().Text($"{r.Hours} h");
                                table.Cell().Text(r.Activity);
                                table.Cell().Text(r.Material ?? "");
                            }
                        });

                        var totalHours = reports.Sum(r => r.Hours);
                        col.Item().PaddingTop(5).Text($"Stunden gesamt: {totalHours} h").Bold();
                    }

                    if (photos.Count > 0)
                    {
                        col.Item().PaddingTop(15).Text("Fotodokumentation").FontSize(14).SemiBold();
                        foreach (var photo in photos)
                        {
                            col.Item().PaddingTop(8).Column(photoCol =>
                            {
                                var filePath = Path.Combine(_uploadRoot, photo.FilePath.Replace('/', Path.DirectorySeparatorChar));
                                if (File.Exists(filePath))
                                {
                                    photoCol.Item().MaxHeight(250).Image(filePath);
                                }

                                var caption = $"{photo.CreatedAt:dd.MM.yyyy HH:mm}";
                                if (!string.IsNullOrEmpty(photo.Description))
                                    caption += $" — {photo.Description}";
                                if (photo.UploadedBy is not null)
                                    caption += $" (von {photo.UploadedBy.Name})";

                                photoCol.Item().PaddingTop(3).Text(caption).FontSize(9).Italic();
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("BaustellenBob — Erstellt am ");
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    text.Span(" — Seite ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
