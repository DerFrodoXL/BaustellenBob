using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IPhotoService
{
    Task<List<PhotoDto>> GetByProjectAsync(Guid projectId);
    Task<PhotoDto> UploadAsync(Guid projectId, string fileName, Stream fileStream, string description, double? latitude, double? longitude, Guid? workReportId = null);
    Task DeleteAsync(Guid id);
}
