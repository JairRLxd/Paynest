using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed class PendingCollectorDebtResponse
{
    public string DebtId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public decimal RemainingAmount { get; init; }
    public bool IsOverdue { get; init; }

    [JsonIgnore]
    public string ClientName => ClientId;

    [JsonIgnore]
    public decimal OutstandingAmount => RemainingAmount;

    [JsonIgnore]
    public decimal ScheduledPaymentAmount => Amount;

    [JsonIgnore]
    public string Status => IsOverdue ? "Vencido" : "Pendiente";
}
