using Paynest.Core.Models.Cobrador.Collections;

namespace Paynest.Core.Interfaces;

public interface ICollectorCollectionsService
{
    Task<CollectorCollectionsDashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<CollectorCollectionsListResponse> GetCollectionsAsync(string filter, CancellationToken ct = default);
}
