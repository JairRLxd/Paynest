using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Collections;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class CollectorCollectionsApiClient(HttpClient http, AuthStateService authState) : ICollectorCollectionsService
{
    public Task<CollectorCollectionsResponse> GetCollectionsAsync(CancellationToken ct = default)
        => authState.CallProtectedAsync(
            token => GetAuthorizedAsync(token, ct),
            ct);

    private async Task<CollectorCollectionsResponse> GetAuthorizedAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/collector/collections");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, "No pudimos cargar los cobros.", ct);

        var result = await response.Content.ReadFromJsonAsync<CollectorCollectionsResponse>(cancellationToken: ct);
        return result!;
    }

    private static async Task<ApiException> CreateApiExceptionAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken ct)
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
        catch { }

        return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
    }
}
