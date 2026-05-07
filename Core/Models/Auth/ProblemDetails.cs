#nullable enable
using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Auth;

public record ProblemDetails(
    [property: JsonPropertyName("status")]             int Status,
    [property: JsonPropertyName("title")]              string Title,
    [property: JsonPropertyName("detail")]             string Detail,
    [property: JsonPropertyName("instance")]           string Instance,
    [property: JsonPropertyName("details")]            Dictionary<string, string[]>? Details = null,
    [property: JsonPropertyName("retryAfterSeconds")]  int? RetryAfterSeconds = null
);
