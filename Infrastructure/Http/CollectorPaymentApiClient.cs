using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class CollectorPaymentApiClient(HttpClient http, AuthStateService authState) : ICollectorPaymentService
{
    public Task<PaymentPreviewResponse> PreviewAsync(string clientId, PreviewPaymentRequest request, CancellationToken ct = default)
        => SendAsync<PaymentPreviewResponse>(HttpMethod.Post, $"api/collector/clients/{clientId}/payments/preview", request, ct);

    public Task<RegisterPaymentResponse> RegisterAsync(string clientId, RegisterPaymentRequest request, CancellationToken ct = default)
        => SendAsync<RegisterPaymentResponse>(HttpMethod.Post, $"api/collector/clients/{clientId}/payments", request, ct);

    public async Task<IReadOnlyList<PaymentHistoryItemModel>> GetHistoryAsync(string clientId, CancellationToken ct = default)
        => await SendAsync<List<PaymentHistoryItemModel>>(HttpMethod.Get, $"api/collector/clients/{clientId}/payments/history", body: null, ct) ?? [];

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
            throw await CreateApiExceptionAsync(response, "No pudimos completar la operación de pago.", ct);

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
