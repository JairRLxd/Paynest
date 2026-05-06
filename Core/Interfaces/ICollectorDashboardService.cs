using Paynest.Core.Models.Cobrador.Dashboard;

namespace Paynest.Core.Interfaces;

public interface ICollectorDashboardService
{
    Task<CollectorDashboardResponse> GetDashboardAsync(CancellationToken ct = default);
}
