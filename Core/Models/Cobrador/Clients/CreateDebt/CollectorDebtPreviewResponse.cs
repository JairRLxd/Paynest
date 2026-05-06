namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtPreviewResponse(
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal TotalAmount,
    decimal ScheduledPaymentAmount,
    decimal LastPaymentAmount,
    int InstallmentsCount,
    DateOnly StartDate,
    DateOnly FirstPaymentDate,
    DateOnly DueDate);
