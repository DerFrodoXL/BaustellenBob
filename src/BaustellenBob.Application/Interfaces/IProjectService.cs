using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<ProjectDto> CreateAsync(ProjectDto dto);
    Task UpdateAsync(ProjectDto dto);
    Task ArchiveAsync(Guid id);
}
