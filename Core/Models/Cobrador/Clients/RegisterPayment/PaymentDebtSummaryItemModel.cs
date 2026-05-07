using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed class PaymentDebtSummaryItemModel
{
    public string DebtId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public decimal PrincipalAmount { get; init; }
    public decimal MoratoryAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public bool HasMoratoryAmount { get; init; }
    public bool IsOverdue { get; init; }

    [JsonIgnore]
    public string Description => string.IsNullOrWhiteSpace(Title) ? DebtId : Title;

    [JsonIgnore]
    public string DueDate => Subtitle;

    [JsonIgnore]
    public string Status => IsOverdue ? "Vencido" : HasMoratoryAmount ? "Parcial" : "Pendiente";
}
