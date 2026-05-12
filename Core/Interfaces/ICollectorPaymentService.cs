using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Core.Models.Cobrador.Payments;

namespace Paynest.Core.Interfaces;

public interface ICollectorPaymentService
{
    Task<PaymentPreviewResponse> PreviewAsync(string clientId, PreviewPaymentRequest request, CancellationToken ct = default);
    Task<RegisterPaymentResponse> RegisterAsync(string clientId, RegisterPaymentRequest request, CancellationToken ct = default);
    Task<byte[]> DownloadTicketAsync(string ticketUrl, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentHistoryItemModel>> GetHistoryAsync(string clientId, CancellationToken ct = default);
    Task<CollectorRecentPaymentsResponse> GetRecentAsync(int limit = 20, CancellationToken ct = default);
    Task<CollectorPaymentSummaryResponse> GetSummaryAsync(CollectorPaymentSummaryRange range, CancellationToken ct = default);
}
