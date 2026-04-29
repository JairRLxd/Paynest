using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record RegisterRequest(
    [property: JsonPropertyName("email")]           string Email,
    [property: JsonPropertyName("fullName")]        string FullName,
    [property: JsonPropertyName("password")]        string Password,
    [property: JsonPropertyName("passwordConfirm")] string PasswordConfirm
);
