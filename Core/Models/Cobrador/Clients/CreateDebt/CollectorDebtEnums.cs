using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Cobrador.Clients.CreateDebt;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Periodicidad
{
    Unica,
    Semanal,
    Quincenal,
    Mensual
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DebtCalculationMode
{
    ByInstallmentAmount,
    ByDueDate
}
