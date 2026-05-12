using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetClientListUseCase(ICollectorClientService clientService)
{
    public Task<CollectorClientListResponse> ExecuteAsync(CancellationToken ct = default)
        => clientService.GetClientsAsync(ct);
}
