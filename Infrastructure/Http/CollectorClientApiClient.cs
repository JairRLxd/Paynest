using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class CollectorClientApiClient(HttpClient http, AuthStateService authState) : ICollectorClientService
{
    public Task<CollectorClientListResponse> GetClientsAsync(CancellationToken ct = default)
        => SendAsync<CollectorClientListResponse>(HttpMethod.Get, "api/collector/clients", body: null, ct);

    public Task<CollectorClientDetailResponse> GetClientDetailAsync(string clientId, CancellationToken ct = default)
        => SendAsync<CollectorClientDetailResponse>(HttpMethod.Get, $"api/collector/clients/{clientId}", body: null, ct);

    public Task<CollectorClientFinancialSummaryResponse> GetFinancialSummaryAsync(string clientId, CancellationToken ct = default)
        => SendAsync<CollectorClientFinancialSummaryResponse>(HttpMethod.Get, $"api/collector/clients/{clientId}/financial-summary", body: null, ct);

    public async Task UpdateClientAsync(string clientId, UpdateClientRequest request, CancellationToken ct = default)
        => await SendAsync<object>(HttpMethod.Put, $"api/collector/clients/{clientId}", request, ct);

    private Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
        => authState.CallProtectedAsync(
            token => SendAuthorizedAsync<T>(method, path, token, body, ct),
            ct);

    private async Task<T> SendAuthorizedAsync<T>(HttpMethod method, string path, string accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, "No pudimos completar la operación de clientes.", ct);

        if (typeof(T) == typeof(object))
            return default!;

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
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
