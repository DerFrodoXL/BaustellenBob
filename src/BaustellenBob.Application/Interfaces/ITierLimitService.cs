using BaustellenBob.Domain;

namespace BaustellenBob.Application.Interfaces;

public interface ITierLimitService
{
    Task<TierLimit> GetCurrentLimitsAsync();
    Task EnsureCanCreateProjectAsync();
    Task EnsureCanCreateEmployeeAsync();
    Task EnsureCanUploadPhotoAsync();
    Task EnsureCanCreateReportAsync();
    Task<bool> CanExportPdfAsync();
    Task<TierUsage> GetUsageAsync();
}

public record TierUsage(int Projects, int Employees, int Photos, int Reports);
