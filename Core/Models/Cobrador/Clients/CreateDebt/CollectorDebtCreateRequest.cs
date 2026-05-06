namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtCreateRequest(
    string Description,
    decimal Amount,
    Periodicidad Periodicidad,
    DebtCalculationMode CalculationMode,
    DateOnly StartDate,
    DateOnly FirstPaymentDate,
    DateOnly DueDate,
    decimal? PaymentAmount,
    decimal? InterestRate,
    decimal? MoratoryRate,
    string? Notes);
