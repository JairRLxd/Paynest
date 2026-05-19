using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Profile;

public record UserProfileResponse(
    [property: JsonPropertyName("visibleName")]   string        VisibleName,
    [property: JsonPropertyName("phone")]         string        Phone,
    [property: JsonPropertyName("postalCode")]    string        PostalCode,
    [property: JsonPropertyName("colonia")]       string        Colonia,
    [property: JsonPropertyName("municipio")]     string        Municipio,
    [property: JsonPropertyName("estado")]        string        Estado,
    [property: JsonPropertyName("address")]       string        Address,
    [property: JsonPropertyName("curp")]          string        Curp,
    [property: JsonPropertyName("rfc")]           string        Rfc,
    [property: JsonPropertyName("operationType")] OperationType OperationType,
    [property: JsonPropertyName("businessName")]  string?       BusinessName = null,
    [property: JsonPropertyName("businessType")]  string?       BusinessType = null
);
