using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

public sealed record CollectorDebtCreateRequest(
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("principalAmount")]
    decimal Amount,
    [property: JsonPropertyName("periodicity")]
    Periodicidad Periodicidad,
    [property: JsonPropertyName("calculationMode")]
    DebtCalculationMode CalculationMode,
    [property: JsonPropertyName("startDate")]
    DateOnly StartDate,
    [property: JsonPropertyName("firstPaymentDate")]
    DateOnly FirstPaymentDate,
    [property: JsonPropertyName("dueDate")]
    DateOnly DueDate,
    [property: JsonPropertyName("paymentAmount")]
    decimal? PaymentAmount,
    [property: JsonPropertyName("interestRate")]
    decimal? InterestRate,
    [property: JsonPropertyName("moratoryRate")]
    decimal? MoratoryRate,
    [property: JsonPropertyName("notes")]
    string? Notes);
