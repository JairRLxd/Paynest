using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class PreviewPaymentUseCase(ICollectorPaymentService paymentService)
{
    public Task<PaymentPreviewResponse> ExecuteAsync(
        string clientId,
        PreviewPaymentRequest request,
        CancellationToken ct = default)
        => paymentService.PreviewAsync(clientId, request, ct);
}
