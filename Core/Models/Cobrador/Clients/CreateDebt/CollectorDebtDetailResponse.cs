using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed class CollectorDebtDetailResponse
{
    public string DebtId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string CollectorUserId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal PrincipalAmount { get; init; }
    public decimal NormalInterestRate { get; init; }
    public decimal NormalInterestAmount { get; init; }
    public decimal MoratoryRate { get; init; }
    public decimal TotalAmount { get; init; }
    public string Periodicity { get; init; } = string.Empty;
    public string CalculationMode { get; init; } = string.Empty;
    public int InstallmentsCount { get; init; }
    public decimal ScheduledInstallmentAmount { get; init; }
    public decimal LastInstallmentAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime FirstPaymentDate { get; init; }
    public DateTime DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }

    [JsonIgnore]
    public string Id => DebtId;

    [JsonIgnore]
    public decimal Amount => PrincipalAmount;

    [JsonIgnore]
    public decimal OutstandingAmount => TotalAmount;

    [JsonIgnore]
    public decimal ScheduledPaymentAmount => ScheduledInstallmentAmount;
}
