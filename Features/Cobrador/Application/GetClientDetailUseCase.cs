using Paynest.Core.Interfaces;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetClientDetailUseCase(
    ICollectorClientService  clientService,
    ICollectorPaymentService paymentService)
{
    public async Task<ClientDetailResult> ExecuteAsync(string clientId, CancellationToken ct = default)
    {
        var detailTask  = clientService.GetClientDetailAsync(clientId, ct);
        var summaryTask = clientService.GetFinancialSummaryAsync(clientId, ct);
        var historyTask = paymentService.GetHistoryAsync(clientId, ct);

        await Task.WhenAll(detailTask, summaryTask, historyTask);

        return new ClientDetailResult(detailTask.Result, summaryTask.Result, historyTask.Result);
    }
}
