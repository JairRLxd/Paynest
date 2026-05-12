namespace Paynest.Core.Models.Cobrador.Agenda;

public sealed record CollectorAgendaDayResponse(
    DateTime Date,
    int TotalCount,
    int ClientsCount,
    decimal TotalAmount,
    IReadOnlyList<CollectorAgendaItemDto> Items);

public sealed record CollectorAgendaItemDto(
    string ClientId,
    string DebtId,
    string InstallmentId,
    int InstallmentNumber,
    string ClientName,
    string Address,
    string Description,
    DateTime DueDate,
    decimal AmountDue,
    string Status,
    string StatusLabel,
    bool IsOverdue,
    bool IsRescheduled = false,
    DateTime? RescheduledAt = null,
    string? RescheduleReason = null);
