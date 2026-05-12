using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Dashboard;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetDashboardUseCase(ICollectorDashboardService dashboardService)
{
    public Task<CollectorDashboardResponse> ExecuteAsync(CancellationToken ct = default)
        => dashboardService.GetDashboardAsync(ct);
}
