using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record UserResponse(
    [property: JsonPropertyName("id")]               string Id,
    [property: JsonPropertyName("email")]            string Email,
    [property: JsonPropertyName("firstName")]        string FirstName,
    [property: JsonPropertyName("lastNameP")]        string LastNameP,
    [property: JsonPropertyName("lastNameM")]        string LastNameM,
    [property: JsonPropertyName("emailVerified")]    bool   EmailVerified,
    [property: JsonPropertyName("role")]             string Role,
    [property: JsonPropertyName("profileCompleted")] bool   ProfileCompleted
);
