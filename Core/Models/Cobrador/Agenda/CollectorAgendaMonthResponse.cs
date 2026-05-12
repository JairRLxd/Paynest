namespace Paynest.Core.Models.Cobrador.Agenda;

public sealed record CollectorAgendaMonthResponse(
    int Year,
    int Month,
    IReadOnlyList<CollectorAgendaDaySummaryDto> Days);

public sealed record CollectorAgendaDaySummaryDto(
    DateTime Date,
    int ItemsCount,
    int ClientsCount,
    decimal TotalAmount,
    int OverdueCount,
    int DueTodayCount,
    int ScheduledCount,
    int RescheduledCount = 0);
