using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record UserResponse(
    [property: JsonPropertyName("id")]             string Id,
    [property: JsonPropertyName("email")]          string Email,
    [property: JsonPropertyName("fullName")]       string FullName,
    [property: JsonPropertyName("emailVerified")]  bool EmailVerified,
    [property: JsonPropertyName("isStaff")]        bool IsStaff,
    [property: JsonPropertyName("isSuperuser")]    bool IsSuperuser
);
