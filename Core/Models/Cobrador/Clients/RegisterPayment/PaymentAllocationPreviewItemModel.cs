namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record PaymentAllocationPreviewItemModel(
    string   DebtId,
    int      InstallmentNumber,
    DateTime DueDate,
    decimal  MoratoryApplied,
    decimal  PrincipalApplied);
