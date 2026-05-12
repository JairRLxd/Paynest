using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Agenda;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class GetAgendaDayUseCase(ICollectorAgendaService agendaService)
{
    public Task<CollectorAgendaDayResponse> ExecuteAsync(DateTime date, string? status = null, CancellationToken ct = default)
        => agendaService.GetDayAsync(date, status, ct);
}
