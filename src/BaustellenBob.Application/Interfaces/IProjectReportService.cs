namespace BaustellenBob.Application.Interfaces;

public interface IProjectReportService
{
    Task<byte[]> GenerateReportAsync(Guid projectId);
}
