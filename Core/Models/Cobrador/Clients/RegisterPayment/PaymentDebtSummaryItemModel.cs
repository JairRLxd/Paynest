namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record PaymentDebtSummaryItemModel(
    string  DebtId,
    string  Description,
    string  DueDate,
    decimal PrincipalAmount,
    decimal MoratoryAmount,
    decimal TotalAmount,
    string  Status);
