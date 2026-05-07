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
            return await ExecuteWithRetryAsync("client debts", async ct =>
            {
                var json = await GetJsonFromAnyAsync(token, ct, "/api/v1/client/debts", "/api/client/debts");
                var debts = ParseDebtGroups(json);
                _logger.LogWarning(
                    "CLIENT_DEBTS_DIAGNOSTIC count={Count} bodyPreview={BodyPreview}",
                    debts.Count,
                    PreviewJson(json));
                return debts;
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<InstallmentDto>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/debts/{Uri.EscapeDataString(groupId)}/installments";
            var v1DetailPath = $"/api/v1/client/debts/{Uri.EscapeDataString(groupId)}";
            var legacyDetailPath = $"/api/client/debts/{Uri.EscapeDataString(groupId)}";
            return await ExecuteWithRetryAsync("client debt installments", async ct =>
            {
                var json = await GetJsonFromAnyAsync(token, ct, path, v1DetailPath, legacyDetailPath);
                var installments = ParseInstallments(groupId, json);
                _logger.LogWarning(
                    "CLIENT_INSTALLMENTS_DIAGNOSTIC debtId={DebtId} count={Count} bodyPreview={BodyPreview}",
                    groupId,
                    installments.Count,
                    PreviewJson(json));
                return installments;
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

                _logger.LogWarning("CLIENT_PAY_DIAGNOSTIC installmentId={InstallmentId} bodyPreview={BodyPreview}", installmentId, PreviewJson(json));
                return ParsePayResponse(json);
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ReceiptDto>> GetReceiptsAsync(CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            return await ExecuteWithRetryAsync("client receipts", async ct =>
            {
                var json = await GetJsonFromAnyAsync(token, ct, "/api/v1/client/receipts", "/api/v1/client/payments", "/api/client/payments");
                return ParseReceipts(json);
            }, cancellationToken);
        }, cancellationToken);
    }

    public Task<ReceiptDto?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
    {
        return _authState.CallProtectedAsync(async token =>
        {
            var path = $"/api/v1/client/receipts/{Uri.EscapeDataString(receiptId)}";
            return await ExecuteWithRetryAsync("client receipt detail", async ct =>
            {
                using var res = await SendAuthorizedAsync(HttpMethod.Get, path, token, ct);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                await EnsureSuccessOrAuthAsync(res, path, ct);
                var json = await res.Content.ReadAsStringAsync(ct);
                return ParseReceipt(json);
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
            const string fallbackPath = "/api/client/collectors/link";
            return await ExecuteWithRetryAsync("client collector link", async ct =>
            {
                using var res = await SendAuthorizedJsonAsync(HttpMethod.Post, path, token, request, ct);
                if (ShouldFallback(res))
                {
                    using var fallbackRes = await SendAuthorizedJsonAsync(HttpMethod.Post, fallbackPath, token, request, ct);
                    await EnsureSuccessOrAuthAsync(fallbackRes, fallbackPath, ct);

                    var fallbackJson = await fallbackRes.Content.ReadAsStringAsync(ct);
                    return JsonSerializer.Deserialize<LinkCollectorResponseDto>(fallbackJson, JsonOpts) ?? new LinkCollectorResponseDto();
                }

                await EnsureSuccessOrAuthAsync(res, path, ct);
                var json = await res.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<LinkCollectorResponseDto>(json, JsonOpts) ?? new LinkCollectorResponseDto();
            }, cancellationToken);
        }, cancellationToken);
    }

    private async Task<string> GetJsonFromAnyAsync(
        string token,
        CancellationToken ct,
        params string[] paths)
    {
        for (var i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            using var res = await SendAuthorizedAsync(HttpMethod.Get, path, token, ct);
            if (ShouldFallback(res) && i < paths.Length - 1)
            {
                continue;
            }

            await EnsureSuccessOrAuthAsync(res, path, ct);
            return await res.Content.ReadAsStringAsync(ct);
        }

        return string.Empty;
    }

    private Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string path,
        string token,
        CancellationToken ct)
    {
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _http.SendAsync(req, ct);
    }

    private Task<HttpResponseMessage> SendAuthorizedJsonAsync<T>(
        HttpMethod method,
        string path,
        string token,
        T body,
        CancellationToken ct)
    {
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(body, options: JsonOpts);
        return _http.SendAsync(req, ct);
    }

    private static bool ShouldFallback(HttpResponseMessage response)
    {
        return response.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.MethodNotAllowed;
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

    private static string PreviewJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "<empty>";
        }

        var compact = string.Join(' ', json.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 700 ? compact : compact[..700];
    }

    private static IReadOnlyList<DebtGroupDto> ParseDebtGroups(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return ParseDebtGroupArray(doc.RootElement);
        }

        if (!TryGetArray(doc.RootElement, out var debts, "debts", "items", "data"))
        {
            return [];
        }

        return ParseDebtGroupArray(debts);
    }

    private static IReadOnlyList<InstallmentDto> ParseInstallments(string groupId, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return ParseInstallmentArray(groupId, doc.RootElement);
        }

        if (!TryGetArray(doc.RootElement, out var schedule, "schedule", "installments", "items", "data"))
        {
            return [];
        }

        return ParseInstallmentArray(groupId, schedule);
    }

    private static IReadOnlyList<DebtGroupDto> ParseDebtGroupArray(JsonElement debts)
    {
        return debts.EnumerateArray()
            .Select(x => new DebtGroupDto
            {
                Id = ReadString(x, "debtId", "id", "_id"),
                Name = ReadString(x, "description", "name", "title"),
                FreelancerName = ReadString(x, "collectorName", "freelancerName"),
                TotalAmount = ReadDecimal(x, "totalAmount", "principalAmount", "amount"),
                PendingAmount = ReadDecimal(x, "remainingAmount", "totalOutstandingAmount", "pendingAmount", "outstandingAmount", "remainingBalance"),
                Frequency = ReadString(x, "periodicity", "frequency")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();
    }

    private static IReadOnlyList<InstallmentDto> ParseInstallmentArray(string groupId, JsonElement installments)
    {
        return installments.EnumerateArray()
            .Select(x =>
            {
                var number = ReadInt(x, "number", "installmentNumber");
                var isPaid = ReadBool(x, "isPaid");
                var isOverdue = ReadBool(x, "isOverdue");
                var status = ReadString(x, "status");
                if (string.IsNullOrWhiteSpace(status))
                    status = isPaid ? "paid" : isOverdue ? "overdue" : "pending";

                return new InstallmentDto
                {
                    Id = ReadString(x, "installmentId", "id", "_id") is { Length: > 0 } id ? id : $"{groupId}:{number}",
                    GroupId = ReadString(x, "groupId", "debtId") is { Length: > 0 } parsedGroupId ? parsedGroupId : groupId,
                    Title = number > 0 ? $"Cuota {number}" : ReadString(x, "title", "description"),
                    DueDate = ReadDateTime(x, "dueDate") ?? DateTime.Today,
                    Amount = ReadDecimal(x, "totalDueAmount", "remainingAmount", "amount", "scheduledAmount"),
                    Status = status
                };
            })
            .ToList();
    }

    private static IReadOnlyList<ReceiptDto> ParseReceipts(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<ReceiptDto>>(json, JsonOpts) ?? [];
        }

        if (TryGetArray(doc.RootElement, out var payments, "items", "payments", "data"))
        {
            return payments.EnumerateArray().Select(PaymentToReceipt).ToList();
        }

        if (TryGetArray(doc.RootElement, out var receipts, "receipts"))
        {
            return receipts.EnumerateArray().Select(ReceiptElementToDto).ToList();
        }

        return [];
    }

    private static ReceiptDto? ParseReceipt(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return ReceiptElementToDto(doc.RootElement);
    }

    private static ReceiptDto ReceiptElementToDto(JsonElement element)
    {
        return new ReceiptDto
        {
            Id = ReadString(element, "receiptId", "id"),
            InstallmentId = ReadString(element, "installmentId"),
            GroupId = ReadString(element, "groupId", "debtId"),
            Title = ReadString(element, "title", "description"),
            Amount = ReadDecimal(element, "amount", "totalAmount"),
            PaidAt = ReadDateTime(element, "paidAt", "createdAt") ?? DateTime.Today,
            Folio = ReadString(element, "folio", "reference"),
            FileUrl = ReadNullableString(element, "fileUrl", "downloadUrl")
        };
    }

    private static ReceiptDto PaymentToReceipt(JsonElement element)
    {
        var paymentId = ReadString(element, "paymentId", "id");
        var allocation = TryGetProperty(element, "allocations", out var allocations) &&
                         allocations.ValueKind == JsonValueKind.Array &&
                         allocations.GetArrayLength() > 0
            ? allocations[0]
            : default;

        var debtId = allocation.ValueKind == JsonValueKind.Object
            ? ReadString(allocation, "debtId")
            : ReadString(element, "debtId");
        var installmentNumber = allocation.ValueKind == JsonValueKind.Object
            ? ReadInt(allocation, "installmentNumber")
            : ReadInt(element, "installmentNumber");

        return new ReceiptDto
        {
            Id = paymentId,
            InstallmentId = installmentNumber > 0 ? $"{debtId}:{installmentNumber}" : string.Empty,
            GroupId = debtId,
            Title = installmentNumber > 0 ? $"Pago de cuota {installmentNumber}" : "Pago registrado",
            Amount = ReadDecimal(element, "amount"),
            PaidAt = ReadDateTime(element, "paidAt", "createdAt") ?? DateTime.Today,
            Folio = ReadString(element, "folio", "reference") is { Length: > 0 } folio ? folio : paymentId,
            FileUrl = ReadNullableString(element, "fileUrl", "proofUrl")
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetArray(JsonElement element, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out value) && value.ValueKind == JsonValueKind.Array)
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string ReadString(JsonElement element, params string[] names)
        => ReadNullableString(element, names) ?? string.Empty;

    private static string? ReadNullableString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        return null;
    }

    private static decimal ReadDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0m;
    }

    private static int ReadInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static bool ReadBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    private static DateTime? ReadDateTime(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
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

            var parsed = JsonSerializer.Deserialize<PayInstallmentResponseDto>(json, JsonOpts);
            if (parsed is null)
            {
                return new PayInstallmentResponseDto();
            }

            if (parsed.Success || parsed.Wallet is not null || parsed.Payment is not null || parsed.Receipt is not null || parsed.Movement is not null)
            {
                return new PayInstallmentResponseDto
                {
                    Success = true,
                    Wallet = parsed.Wallet,
                    Payment = parsed.Payment,
                    Receipt = parsed.Receipt,
                    Movement = parsed.Movement
                };
            }

            return parsed;
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
