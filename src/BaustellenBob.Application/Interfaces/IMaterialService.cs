using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IMaterialService
{
    Task<List<MaterialEntryDto>> GetByProjectAsync(Guid projectId);
    Task<List<string>> GetSuggestionsAsync(string query, CancellationToken cancellationToken = default);
    Task<MaterialEntryDto> CreateAsync(Guid projectId, MaterialEntryDto dto);
    Task UpdateAsync(MaterialEntryDto dto);
    Task DeleteAsync(Guid id);
}
