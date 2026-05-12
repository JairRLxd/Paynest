using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Core.Models.Cobrador.Payments;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public class CollectorPaymentApiClient(
    HttpClient http,
    AuthStateService authState,
    ILogger<CollectorPaymentApiClient> logger) : ICollectorPaymentService
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public Task<PaymentPreviewResponse> PreviewAsync(string clientId, PreviewPaymentRequest request, CancellationToken ct = default)
        => SendAsync<PaymentPreviewResponse>(HttpMethod.Post, $"/api/v1/collector/clients/{clientId}/payments/preview", request, ct);

    public Task<RegisterPaymentResponse> RegisterAsync(string clientId, RegisterPaymentRequest request, CancellationToken ct = default)
        => SendAsync<RegisterPaymentResponse>(HttpMethod.Post, $"/api/v1/collector/clients/{clientId}/payments", request, ct);

    public Task<byte[]> DownloadTicketAsync(string ticketUrl, CancellationToken ct = default)
        => authState.CallProtectedAsync(
            token => DownloadTicketAuthorizedAsync(ticketUrl, token, ct),
            ct);

    public async Task<IReadOnlyList<PaymentHistoryItemModel>> GetHistoryAsync(string clientId, CancellationToken ct = default)
        => await SendAsync<List<PaymentHistoryItemModel>>(HttpMethod.Get, $"/api/v1/collector/clients/{clientId}/payments/history", body: null, ct) ?? [];

    public Task<CollectorRecentPaymentsResponse> GetRecentAsync(int limit = 20, CancellationToken ct = default)
        => SendAsync<CollectorRecentPaymentsResponse>(HttpMethod.Get, $"/api/collector/payments/recent?limit={limit}", body: null, ct);

    public Task<CollectorPaymentSummaryResponse> GetSummaryAsync(CollectorPaymentSummaryRange range, CancellationToken ct = default)
        => SendAsync<CollectorPaymentSummaryResponse>(HttpMethod.Get, $"/api/collector/payments/summary?range={ToQuery(range)}", body: null, ct);

    private Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
        => authState.CallProtectedAsync(
            token => SendAuthorizedAsync<T>(method, path, token, body, ct),
            ct);

    private async Task<T> SendAuthorizedAsync<T>(HttpMethod method, string path, string accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string? requestBody = null;
        if (body is not null)
        {
            requestBody = JsonSerializer.Serialize(body, _jsonOpts);
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
        }

        logger.LogDebug("COLLECTOR_PAY_REQ  {Method} {Path} body={Body}", method, path, requestBody ?? "(none)");

        var response = await http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        logger.LogDebug("COLLECTOR_PAY_RESP {Method} {Path} status={Status} body={Body}",
            method, path, (int)response.StatusCode, responseBody);

        if (!response.IsSuccessStatusCode)
        {
            var message = ExtractMessage(responseBody) ?? "No pudimos completar la operación de pago.";
            logger.LogWarning("COLLECTOR_PAY_FAIL {Method} {Path} status={Status} error={Error}",
                method, path, (int)response.StatusCode, responseBody);
            throw new ApiException(message, (int)response.StatusCode);
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseBody))
            return default!;

        var result = JsonSerializer.Deserialize<T>(responseBody, _jsonOpts);
        return result!;
    }

    private async Task<byte[]> DownloadTicketAuthorizedAsync(string ticketUrl, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ticketUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException(ExtractMessage(body) ?? "No se pudo descargar el ticket.", (int)response.StatusCode);
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static string ToQuery(CollectorPaymentSummaryRange range) => range switch
    {
        CollectorPaymentSummaryRange.Today => "today",
        CollectorPaymentSummaryRange.Week => "week",
        CollectorPaymentSummaryRange.Month => "month",
        _ => "today"
    };

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

        return body.Trim();
    }
}
