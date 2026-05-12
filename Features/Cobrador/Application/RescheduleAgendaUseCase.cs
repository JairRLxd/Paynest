using Paynest.Core.Interfaces;

namespace Paynest.Features.Cobrador.UseCases;

public sealed class RescheduleAgendaUseCase(ICollectorAgendaService agendaService)
{
    public Task ExecuteAsync(string debtId, int installmentNumber, DateTime newDueDate, string? reason, CancellationToken ct = default)
        => agendaService.RescheduleAsync(debtId, installmentNumber, newDueDate, reason, ct);
}
