using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed class CollectorDebtOpenSummaryResponse
{
    public string ClientId { get; init; } = string.Empty;
    public int ActiveDebtsCount { get; init; }
    public int OpenInstallmentsCount { get; init; }
    public decimal TotalOutstandingAmount { get; init; }
    public decimal TotalOverdueAmount { get; init; }
    public DateTime? NextDueDate { get; init; }
    public IReadOnlyList<CollectorDebtOpenSummaryItemResponse> OpenInstallments { get; init; } = [];

    [JsonIgnore]
    public int OpenDebtsCount => ActiveDebtsCount;

    [JsonIgnore]
    public IReadOnlyList<CollectorDebtOpenSummaryItemResponse> Debts => OpenInstallments;
}

public sealed class CollectorDebtOpenSummaryItemResponse
{
    public string DebtId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public decimal RemainingAmount { get; init; }
    public bool IsOverdue { get; init; }

    [JsonIgnore]
    public decimal OutstandingAmount => RemainingAmount;

    [JsonIgnore]
    public decimal ScheduledPaymentAmount => Amount;

    [JsonIgnore]
    public string Status => IsOverdue ? "Vencido" : "Pendiente";
}
