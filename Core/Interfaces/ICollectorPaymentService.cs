using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

namespace Paynest.Core.Interfaces;

public interface ICollectorPaymentService
{
    Task<PaymentPreviewResponse> PreviewAsync(string clientId, PreviewPaymentRequest request, CancellationToken ct = default);
    Task<RegisterPaymentResponse> RegisterAsync(string clientId, RegisterPaymentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentHistoryItemModel>> GetHistoryAsync(string clientId, CancellationToken ct = default);
}
