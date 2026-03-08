using BaustellenBob.Application.DTOs;

namespace BaustellenBob.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}
