using System.Text.Json.Serialization;

namespace Paynest.Core.Models.Profile;

public record DocumentStatusItem(
    [property: JsonPropertyName("type")]       string           Type,
    [property: JsonPropertyName("label")]      string           Label,
    [property: JsonPropertyName("uploaded")]   bool             Uploaded,
    [property: JsonPropertyName("uploadedAt")] DateTimeOffset?  UploadedAt,
    [property: JsonPropertyName("url")]        string?          Url = null
);

public record DocumentsStatusResponse(
    [property: JsonPropertyName("documents")]     List<DocumentStatusItem> Documents,
    [property: JsonPropertyName("uploadedCount")] int                      UploadedCount,
    [property: JsonPropertyName("requiredCount")] int                      RequiredCount,
    [property: JsonPropertyName("completed")]     bool                     Completed
);
