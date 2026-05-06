namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtOpenSummaryResponse(
    string ClientId,
    decimal TotalOutstandingAmount,
    int OpenDebtsCount,
    IReadOnlyList<CollectorDebtOpenSummaryItemResponse> Debts);

public sealed record CollectorDebtOpenSummaryItemResponse(
    string DebtId,
    string Description,
    decimal OutstandingAmount,
    decimal ScheduledPaymentAmount,
    DateOnly DueDate,
    string Status);
