using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Core.Models.Profile;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class ProfileApiClient(HttpClient http, AuthStateService authState) : IProfileService
{
    public async Task SavePersonalInfoAsync(UserProfileRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/profile/personal");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.AccessToken);
        req.Content = JsonContent.Create(request);
        var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task UploadDocumentAsync(DocumentType type, PreparedUploadFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(file.Content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent(type.ToString()), "documentType");

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/profile/documents");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.AccessToken);
        req.Content = content;
        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(res, "No pudimos subir el documento.", ct);
    }

    public async Task SavePaymentConfigAsync(PaymentConfigRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/profile/payment-config");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.AccessToken);
        req.Content = JsonContent.Create(request);
        var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<ApiException> CreateApiExceptionAsync(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var message = ExtractMessage(body) ?? fallbackMessage;
        return new ApiException(message, (int)response.StatusCode);
    }

    private static string? ExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var parsed = JsonSerializer.Deserialize<ProblemDetails>(body);
            if (!string.IsNullOrWhiteSpace(parsed?.Detail))
                return parsed.Detail;
        }
        catch
        {
            // ignored
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            foreach (var name in new[] { "detail", "message", "title", "error" })
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        catch
        {
            // ignored
        }

        return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
    }
}
