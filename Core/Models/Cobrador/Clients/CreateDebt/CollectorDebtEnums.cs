using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Periodicidad
{
    Weekly,
    Biweekly,
    Monthly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DebtCalculationMode
{
    ByInstallmentAmount,
    ByDueDate
}
