using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IProjectAssignmentService
{
    Task<List<ProjectAssignmentDto>> GetByProjectAsync(Guid projectId);
    Task<List<ProjectAssignmentDto>> GetByWeekAsync(DateTime weekStart);
    Task<ProjectAssignmentDto> CreateAsync(ProjectAssignmentDto dto);
    Task UpdateAsync(ProjectAssignmentDto dto);
    Task DeleteAsync(Guid id);
}
