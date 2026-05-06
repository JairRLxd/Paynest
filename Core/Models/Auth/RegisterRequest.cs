using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record RegisterRequest(
    [property: JsonPropertyName("email")]           string Email,
    [property: JsonPropertyName("firstName")]       string FirstName,
    [property: JsonPropertyName("lastNameP")]       string LastNameP,
    [property: JsonPropertyName("lastNameM")]       string LastNameM,
    [property: JsonPropertyName("password")]        string Password,
    [property: JsonPropertyName("passwordConfirm")] string PasswordConfirm
);
