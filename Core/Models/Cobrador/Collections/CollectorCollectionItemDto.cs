namespace Paynest.Core.Models.Cobrador.Collections;

public sealed record CollectorCollectionItemDto(
    string   DebtId,
    string   ClientId,
    string   ClientName,
    string   ClientInitials,
    string   Description,
    int      InstallmentNumber,
    DateTime DueDate,
    string   DueDateDisplay,
    decimal  ScheduledAmount,
    decimal  RemainingAmount,
    decimal  MoratoryAmount,
    decimal  MoratoryRate,
    decimal  TotalDueAmount,
    string   StatusLabel,
    bool     HasMoratory,
    bool     HasPartialPayment,
    bool     IsOverdue,
    bool     IsDueToday = false,
    bool     IsInThisWeek = false);
