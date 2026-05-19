using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Profile;

public record PaymentConfigResponse(
    [property: JsonPropertyName("efectivoEnabled")]     bool    EfectivoEnabled,
    [property: JsonPropertyName("transferenciaEnabled")] bool    TransferenciaEnabled,
    [property: JsonPropertyName("bankName")]            string? BankName,
    [property: JsonPropertyName("accountHolder")]       string? AccountHolder,
    [property: JsonPropertyName("clabe")]               string? Clabe,
    [property: JsonPropertyName("terminalEnabled")]     bool    TerminalEnabled,
    [property: JsonPropertyName("terminalProvider")]    string? TerminalProvider,
    [property: JsonPropertyName("terminalReference")]   string? TerminalReference
);
