using Paynest.Core.Models.Cobrador.Clients.CreateDebt;

namespace Paynest.Core.Interfaces;

public interface ICollectorDebtService
{
    Task<CollectorDebtPreviewResponse> PreviewAsync(string clientId, CollectorDebtPreviewRequest request, CancellationToken ct = default);
    Task<CollectorDebtDetailResponse> CreateAsync(string clientId, CollectorDebtCreateRequest request, CancellationToken ct = default);
    Task<CollectorDebtDetailResponse> GetDebtAsync(string clientId, string debtId, CancellationToken ct = default);
    Task<CollectorDebtDetailResponse> UpdateAsync(string clientId, string debtId, CollectorDebtCreateRequest request, CancellationToken ct = default);
    Task<CollectorDebtOpenSummaryResponse> GetOpenSummaryAsync(string clientId, CancellationToken ct = default);
    Task<IReadOnlyList<PendingCollectorDebtResponse>> GetPendingAsync(CancellationToken ct = default);
}
