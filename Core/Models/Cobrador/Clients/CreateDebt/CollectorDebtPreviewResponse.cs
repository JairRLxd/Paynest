using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed class CollectorDebtPreviewResponse
{
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
    public string InstallmentTitle { get; init; } = string.Empty;
    public string Footnote { get; init; } = string.Empty;

    [JsonIgnore]
    public decimal InterestAmount => NormalInterestAmount;

    [JsonIgnore]
    public decimal ScheduledPaymentAmount => ScheduledInstallmentAmount;

    [JsonIgnore]
    public decimal LastPaymentAmount => LastInstallmentAmount;
}
