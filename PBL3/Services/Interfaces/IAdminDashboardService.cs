using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetOverviewAsync(CancellationToken cancellationToken = default);
}
