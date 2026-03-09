using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(UserDto dto);
    Task UpdateAsync(UserDto dto);
    Task DeleteAsync(Guid id);
    Task<string> UploadProfilePictureAsync(Guid userId, string fileName, Stream stream);
    Task<string?> GetProfilePicturePathAsync(Guid userId);
}
