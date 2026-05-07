#nullable enable
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Client.Api;

public sealed class HttpDebtApiClient(HttpClient httpClient, AuthStateService authState, ILogger<HttpDebtApiClient> logger) : IDebtApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan[] RetryBackoff = [TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(600)];

    private readonly HttpClient _http = httpClient;
    private readonly AuthStateService _authState = authState;
    private readonly ILogger<HttpDebtApiClient> _logger = logger;

    public Task<IReadOnlyList<DebtGroupDto>> GetDebtGroupsAsync(CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/debts", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/client/debts");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/debts", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                var data = JsonSerializer.Deserialize<List<DebtGroupDto>>(json, JsonOpts) ?? [];
                return (IReadOnlyList<DebtGroupDto>)data;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<InstallmentDto>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/debts/{Uri.EscapeDataString(groupId)}/installments";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, path, ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                var data = JsonSerializer.Deserialize<List<InstallmentDto>>(json, JsonOpts) ?? [];
                return (IReadOnlyList<InstallmentDto>)data;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<PayInstallmentResponseDto> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/installments/{Uri.EscapeDataString(installmentId)}/pay";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(new PayInstallmentRequestDto(), options: JsonOpts);

                using var res = await _http.SendAsync(req, ct);
                if (!res.IsSuccessStatusCode)
                {
                    await EnsureSuccessOrAuthAsync(res, path, ct);
                    return new PayInstallmentResponseDto();
                }

                if (res.Content.Headers.ContentLength is 0)
                {
                    return new PayInstallmentResponseDto { Success = true };
                }

                var json = await res.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new PayInstallmentResponseDto { Success = true };
                }

                return ParsePayResponse(json);
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ReceiptDto>> GetReceiptsAsync(CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/receipts", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/client/receipts");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/receipts", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                var data = JsonSerializer.Deserialize<List<ReceiptDto>>(json, JsonOpts) ?? [];
                return (IReadOnlyList<ReceiptDto>)data;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<ReceiptDto?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/receipts/{Uri.EscapeDataString(receiptId)}";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                await EnsureSuccessOrAuthAsync(res, path, ct);
                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<ReceiptDto>(json, JsonOpts);
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/receipts/{Uri.EscapeDataString(receiptId)}/download";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if ((int)res.StatusCode is >= 300 and < 400 && res.Headers.Location is { } location)
                {
                    return location.IsAbsoluteUri ? location.ToString() : new Uri(_http.BaseAddress!, location).ToString();
                }

                await EnsureSuccessOrAuthAsync(res, path, ct);
                return res.RequestMessage?.RequestUri?.ToString();
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/notification-preferences", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/client/notification-preferences");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/notification-preferences", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<NotificationPreferencesDto>(json, JsonOpts) ?? new NotificationPreferencesDto();
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(
        NotificationPreferencesDto request,
        CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/notification-preferences", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/client/notification-preferences");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(request, options: JsonOpts);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/notification-preferences", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<NotificationPreferencesDto>(json, JsonOpts) ?? request;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<WalletDto> GetWalletAsync(CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/wallet", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/client/wallet");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/wallet", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<WalletDto>(json, JsonOpts) ?? new WalletDto();
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<WalletMovementDto>> GetWalletMovementsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var safeLimit = Math.Clamp(limit, 1, 100);
            var path = $"/api/v1/client/wallet/movements?limit={safeLimit}";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, path, ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                var data = JsonSerializer.Deserialize<List<WalletMovementDto>>(json, JsonOpts) ?? [];
                return (IReadOnlyList<WalletMovementDto>)data;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<WalletDepositResponseDto> DepositWalletAsync(
        WalletDepositRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("/api/v1/client/wallet/deposit", async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/client/wallet/deposit");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(request, options: JsonOpts);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, "/api/v1/client/wallet/deposit", ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<WalletDepositResponseDto>(json, JsonOpts) ?? new WalletDepositResponseDto();
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<LinkCollectorResponseDto> LinkCollectorAsync(
        LinkCollectorRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            const string path = "/api/v1/client/collectors/link";
            return await ExecuteWithRetryAsync(path, async ct =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, path);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(request, options: JsonOpts);

                using var res = await _http.SendAsync(req, ct);
                await EnsureSuccessOrAuthAsync(res, path, ct);

                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<LinkCollectorResponseDto>(json, JsonOpts) ?? new LinkCollectorResponseDto();
            }, cancellationToken);
        }, cancellationToken);
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        string endpoint,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(RequestTimeout);
            var start = DateTime.UtcNow;

            try
            {
                var result = await action(timeoutCts.Token);
                var elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                _logger.LogInformation("Client API {Endpoint} attempt={Attempt} elapsedMs={ElapsedMs:F0}", endpoint, attempt, elapsedMs);
                return result;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt <= RetryBackoff.Length + 1)
            {
                var elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                _logger.LogWarning(ex, "Transient error at {Endpoint} attempt={Attempt} elapsedMs={ElapsedMs:F0}", endpoint, attempt, elapsedMs);

                if (attempt > RetryBackoff.Length)
                {
                    throw;
                }

                await Task.Delay(RetryBackoff[attempt - 1], cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TaskCanceledException || ex is TimeoutException || ex is HttpRequestException;
    }

    private static PayInstallmentResponseDto ParsePayResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.True)
            {
                return new PayInstallmentResponseDto { Success = true };
            }

            if (doc.RootElement.ValueKind == JsonValueKind.False)
            {
                return new PayInstallmentResponseDto();
            }

            return JsonSerializer.Deserialize<PayInstallmentResponseDto>(json, JsonOpts) ?? new PayInstallmentResponseDto();
        }
        catch
        {
            return new PayInstallmentResponseDto();
        }
    }

    private static async Task EnsureSuccessOrAuthAsync(HttpResponseMessage response, string path, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new AuthException(new ProblemDetails(
                Status: 401,
                Title: "Unauthorized",
                Detail: "Token inválido o expirado.",
                Instance: path));
        }

        var detail = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(detail) ? $"HTTP {(int)response.StatusCode}" : detail,
            null,
            response.StatusCode);
    }
}
