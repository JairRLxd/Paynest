using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record AuthResponse(
    [property: JsonPropertyName("accessToken")]  string AccessToken,
    [property: JsonPropertyName("tokenType")]    string TokenType,
    [property: JsonPropertyName("expiresIn")]    int ExpiresIn,
    [property: JsonPropertyName("user")]         UserResponse User
);
