using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Agenda;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetAgendaMonthUseCase(ICollectorAgendaService agendaService)
{
    public Task<CollectorAgendaMonthResponse> ExecuteAsync(int year, int month, CancellationToken ct = default)
        => agendaService.GetMonthAsync(year, month, ct);
}
