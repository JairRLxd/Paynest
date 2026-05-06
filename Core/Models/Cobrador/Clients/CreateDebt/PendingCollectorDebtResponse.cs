namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record PendingCollectorDebtResponse(
    string DebtId,
    string ClientId,
    string ClientName,
    string Description,
    decimal OutstandingAmount,
    decimal ScheduledPaymentAmount,
    DateOnly DueDate,
    string Status);
