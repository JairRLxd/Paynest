using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Collections;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetCollectionsDashboardUseCase(ICollectorCollectionsService collectionsService)
{
    public Task<CollectorCollectionsDashboardResponse> ExecuteAsync(CancellationToken ct = default)
        => collectionsService.GetDashboardAsync(ct);
}
