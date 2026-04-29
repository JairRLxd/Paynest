using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record LoginRequest(
    [property: JsonPropertyName("email")]    string Email,
    [property: JsonPropertyName("password")] string Password
);
