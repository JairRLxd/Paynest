namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtDetailResponse(
    string Id,
    string ClientId,
    string Description,
    decimal Amount,
    decimal TotalAmount,
    decimal OutstandingAmount,
    decimal ScheduledPaymentAmount,
    int InstallmentsCount,
    Periodicidad Periodicidad,
    DebtCalculationMode CalculationMode,
    DateOnly StartDate,
    DateOnly FirstPaymentDate,
    DateOnly DueDate,
    decimal? InterestRate,
    decimal? MoratoryRate,
    string? Notes,
    string Status);
