using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IWorkReportService
{
    Task<List<WorkReportDto>> GetByProjectAsync(Guid projectId);
    Task<WorkReportDto> CreateAsync(Guid projectId, WorkReportDto dto);
    Task UpdateAsync(WorkReportDto dto);
    Task DeleteAsync(Guid id);
}
