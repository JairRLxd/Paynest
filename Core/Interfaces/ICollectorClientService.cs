using Paynest.Core.Models.Cobrador.Clients;

namespace Paynest.Core.Interfaces;

public interface ICollectorClientService
{
    Task<CollectorClientListResponse> GetClientsAsync(CancellationToken ct = default);
    Task<CollectorClientDetailResponse> GetClientDetailAsync(string clientId, CancellationToken ct = default);
    Task<CollectorClientFinancialSummaryResponse> GetFinancialSummaryAsync(string clientId, CancellationToken ct = default);
    Task UpdateClientAsync(string clientId, UpdateClientRequest request, CancellationToken ct = default);
}
