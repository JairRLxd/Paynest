namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtPreviewRequest(
    decimal Amount,
    Periodicidad Periodicidad,
    DebtCalculationMode CalculationMode,
    DateOnly StartDate,
    DateOnly FirstPaymentDate,
    DateOnly DueDate,
    decimal? PaymentAmount,
    decimal? InterestRate,
    decimal? MoratoryRate);
