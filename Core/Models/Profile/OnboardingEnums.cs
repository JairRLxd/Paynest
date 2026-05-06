using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Profile;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationType { Persona, Empresa }

public enum DocumentType
{
    IdentificacionOficialFrente,
    IdentificacionOficialReverso,
    Selfie,
    ComprobanteDomicilioFrente,
    ComprobanteDomicilioReverso
}

public enum PaymentMethodType { Efectivo, Transferencia, Terminal }

// Escalable: agrega BusinessOwner, Employee, etc. cuando sea necesario
public enum UserRole { AdminCollector }
