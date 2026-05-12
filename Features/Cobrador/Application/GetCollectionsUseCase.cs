using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Collections;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetCollectionsUseCase(ICollectorCollectionsService collectionsService)
{
    public Task<CollectorCollectionsListResponse> ExecuteAsync(string filter, CancellationToken ct = default)
        => collectionsService.GetCollectionsAsync(filter, ct);
}
