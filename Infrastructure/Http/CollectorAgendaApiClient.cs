using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Agenda;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Infrastructure.Http;

public sealed class CollectorAgendaApiClient(HttpClient http, AuthStateService authState) : ICollectorAgendaService
{
    public Task<CollectorAgendaMonthResponse> GetMonthAsync(int year, int month, CancellationToken ct = default)
        => SendAsync<CollectorAgendaMonthResponse>(HttpMethod.Get, $"/api/v1/collector/agenda?year={year}&month={month}", body: null, ct);

    public Task<CollectorAgendaDayResponse> GetDayAsync(DateTime date, string? status = null, CancellationToken ct = default)
    {
        var path = $"/api/v1/collector/agenda/day?date={date:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            path += $"&status={status}";
        return SendAsync<CollectorAgendaDayResponse>(HttpMethod.Get, path, body: null, ct);
    }

    public Task RescheduleAsync(string debtId, int installmentNumber, DateTime newDueDate, string? reason, CancellationToken ct = default)
        => SendVoidAsync(HttpMethod.Patch, "/api/v1/collector/agenda/reschedule", new
        {
            debtId,
            installmentNumber,
            newDueDate = newDueDate.ToString("yyyy-MM-dd"),
            reason
        }, ct);

    private Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
        => authState.CallProtectedAsync(
            token => SendAuthorizedAsync<T>(method, path, token, body, ct),
            ct);

    private Task SendVoidAsync(HttpMethod method, string path, object? body, CancellationToken ct)
        => authState.CallProtectedAsync(
            async token =>
            {
                await SendAuthorizedVoidAsync(method, path, token, body, ct);
                return true;
            },
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
            throw await CreateApiExceptionAsync(response, "No pudimos cargar la agenda.", ct);

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return result!;
    }

    private async Task SendAuthorizedVoidAsync(HttpMethod method, string path, string accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, "No pudimos reprogramar el cobro.", ct);
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

        return body.Trim();
    }
}
