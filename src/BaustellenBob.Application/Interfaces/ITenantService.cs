namespace BaustellenBob.Application.Interfaces;

public interface ITenantService
{
    Task<string?> GetLogoPathAsync();
    Task<string> UploadLogoAsync(string fileName, Stream stream);
}
