using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class RegisterPaymentUseCase(ICollectorPaymentService paymentService)
{
    public Task<RegisterPaymentResponse> ExecuteAsync(
        string clientId,
        RegisterPaymentRequest request,
        CancellationToken ct = default)
        => paymentService.RegisterAsync(clientId, request, ct);
}
