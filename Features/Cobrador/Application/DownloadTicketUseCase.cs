using Paynest.Core.Interfaces;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class DownloadTicketUseCase(ICollectorPaymentService paymentService)
{
    public Task<byte[]> ExecuteAsync(string ticketUrl, CancellationToken ct = default)
        => paymentService.DownloadTicketAsync(ticketUrl, ct);
}
