using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Payments;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetRecentCollectorPaymentsUseCase(ICollectorPaymentService paymentService)
{
    public Task<CollectorRecentPaymentsResponse> ExecuteAsync(int limit = 5, CancellationToken ct = default)
        => paymentService.GetRecentAsync(limit, ct);
}
