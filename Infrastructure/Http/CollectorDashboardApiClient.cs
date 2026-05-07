using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Core.Models.Cobrador.Dashboard;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class CollectorDashboardApiClient(HttpClient http, AuthStateService authState) : ICollectorDashboardService
{
    public Task<CollectorDashboardResponse> GetDashboardAsync(CancellationToken ct = default)
        => authState.CallProtectedAsync(
            token => GetAuthorizedAsync(token, ct),
            ct);

    private async Task<CollectorDashboardResponse> GetAuthorizedAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/collector/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, "No pudimos cargar el resumen.", ct);

        var result = await response.Content.ReadFromJsonAsync<CollectorDashboardResponse>(cancellationToken: ct);
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
            var parsed = JsonSerializer.Deserialize<ProblemDetails>(body);
            if (!string.IsNullOrWhiteSpace(parsed?.Detail))
                return parsed.Detail;
        }
        catch { }

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
