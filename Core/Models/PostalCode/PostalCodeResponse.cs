using System.Text.Json.Serialization;

namespace Paynest.Core.Models.PostalCode;

public record PostalCodeResponse(
    [property: JsonPropertyName("codigoPostal")] string        CodigoPostal,
    [property: JsonPropertyName("estado")]       string        Estado,
    [property: JsonPropertyName("municipio")]    string        Municipio,
    [property: JsonPropertyName("colonias")]     List<Colonia> Colonias
);

public record Colonia(
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("tipo")]   string Tipo
);
