using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record AuthResponse(
    [property: JsonPropertyName("accessToken")]      string AccessToken,
    [property: JsonPropertyName("refreshToken")]     string RefreshToken,
    [property: JsonPropertyName("tokenType")]        string TokenType,
    [property: JsonPropertyName("expiresIn")]        int    ExpiresIn,
    [property: JsonPropertyName("refreshExpiresIn")] int    RefreshExpiresIn,
    [property: JsonPropertyName("id")]               string Id,
    [property: JsonPropertyName("email")]            string Email,
    [property: JsonPropertyName("firstName")]        string FirstName,
    [property: JsonPropertyName("lastNameP")]        string LastNameP,
    [property: JsonPropertyName("lastNameM")]        string LastNameM,
    [property: JsonPropertyName("emailVerified")]    bool   EmailVerified,
    [property: JsonPropertyName("role")]             string Role,
    [property: JsonPropertyName("profileCompleted")] bool   ProfileCompleted,
    [property: JsonPropertyName("user")]             UserResponse? User
);
