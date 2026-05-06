namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record PaymentPreviewResponse(
    string   ClientId,
    decimal  TotalDebtAmount,
    decimal  CurrentDueAmount,
    decimal  ScheduledPaymentAmount,
    decimal  OverduePrincipalAmount,
    decimal  CurrentMoratoryAmount,
    decimal  MoratoryRatePercent,
    decimal  SuggestedAmountToPay,
    bool     IsPartialPaymentAllowed,
    string   StatusLabel,
    IReadOnlyList<PaymentDebtSummaryItemModel>?      DebtSummaryItems,
    IReadOnlyList<PaymentAllocationPreviewItemModel>? AllocationPreview);
