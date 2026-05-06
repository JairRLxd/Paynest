using Paynest.Core.Models.Cobrador.Collections;

namespace Paynest.Core.Interfaces;

public interface ICollectorCollectionsService
{
    Task<CollectorCollectionsResponse> GetCollectionsAsync(CancellationToken ct = default);
}
