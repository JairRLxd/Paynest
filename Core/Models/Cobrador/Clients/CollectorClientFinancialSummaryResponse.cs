namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record CollectorClientFinancialSummaryResponse(
    string    ClientId,
    decimal   TotalDebtAmount,
    decimal   TotalPaidAmount,
    decimal   CurrentDueAmount,
    decimal   OverduePrincipalAmount,
    decimal   CurrentMoratoryAmount,
    decimal   ScheduledPaymentAmount,
    decimal   MoratoryRatePercent,
    string    StatusLabel,         // "Atrasado" | "Pendiente" | "Al corriente" | "Liquidado"
    string    StatusDescription,
    bool      IsPartialPaymentAllowed,
    DateTime? NextDueDate);
