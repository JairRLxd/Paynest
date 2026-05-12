using Paynest.Core.Models.Cobrador.Agenda;

namespace Paynest.Core.Interfaces;

public interface ICollectorAgendaService
{
    Task<CollectorAgendaMonthResponse> GetMonthAsync(int year, int month, CancellationToken ct = default);
    Task<CollectorAgendaDayResponse> GetDayAsync(DateTime date, string? status = null, CancellationToken ct = default);
    Task RescheduleAsync(string debtId, int installmentNumber, DateTime newDueDate, string? reason, CancellationToken ct = default);
}
