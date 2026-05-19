#nullable enable
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Infrastructure.Http;

public class AuthApiClient : IAuthService
{
    private const string BasePath = "/api/v1/auth";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(HttpClient http, ILogger<AuthApiClient> logger)
    {
        _http = http;
        _logger = logger;
        _logger.LogInformation("AuthApiClient initialized with BaseAddress={BaseAddress}", _http.BaseAddress);
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => PostAsync<AuthResponse>($"{BasePath}/login", request, ct);

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => PostAsync<AuthResponse>($"{BasePath}/register", request, ct);

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/forgot-password");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOpts), Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowAuthExceptionAsync(res, $"{BasePath}/forgot-password", ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/reset-password");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOpts), Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowAuthExceptionAsync(res, $"{BasePath}/reset-password", ct);
    }

    public Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
        => PostAsync<AuthResponse>($"{BasePath}/refresh", new RefreshTokenRequest(refreshToken), ct);

    public async Task LogoutAsync(string? refreshToken = null, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/logout");
        if (!string.IsNullOrWhiteSpace(refreshToken))
            req.Content = new StringContent(
                JsonSerializer.Serialize(new RefreshTokenRequest(refreshToken), JsonOpts),
                Encoding.UTF8,
                "application/json");

        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowAuthExceptionAsync(res, $"{BasePath}/logout", ct);
    }

    public async Task<UserResponse> MeAsync(string accessToken, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowAuthExceptionAsync(res, $"{BasePath}/me", ct);
        return (await DeserializeAsync<UserResponse>(res, ct))!;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<T> PostAsync<T>(string path, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOpts);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("POST {Url} body={Body}", BuildAbsoluteUri(path), json);
        }
        else
        {
            _logger.LogInformation("POST {Url} without body", BuildAbsoluteUri(path));
        }

        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowAuthExceptionAsync(res, path, ct);

        return (await DeserializeAsync<T>(res, ct))!;
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage res, CancellationToken ct)
    {
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private async Task ThrowAuthExceptionAsync(HttpResponseMessage res, string path, CancellationToken ct)
    {
        var json = await res.Content.ReadAsStringAsync(ct);
        var problem = ParseProblem(json, (int)res.StatusCode);

        _logger.LogWarning(
            "HTTP {StatusCode} at {Url}. ResponseBody={Body}",
            (int)res.StatusCode,
            BuildAbsoluteUri(path),
            string.IsNullOrWhiteSpace(json) ? "<empty>" : json);

        // Si el header Retry-After llega y el body no trae retryAfterSeconds, lo leemos
        if (problem.RetryAfterSeconds is null &&
            res.Headers.RetryAfter?.Delta is { } delta)
        {
            problem = problem with { RetryAfterSeconds = (int)delta.TotalSeconds };
        }

        throw new AuthException(problem);
    }

    private Uri BuildAbsoluteUri(string path) => new(_http.BaseAddress!, path);

    private static ProblemDetails ParseProblem(string? json, int status)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ProblemDetails(
                Status: status,
                Title: "Error",
                Detail: $"La API devolvió HTTP {status} sin body.",
                Instance: string.Empty);

        try
        {
            var parsed = JsonSerializer.Deserialize<ProblemDetails>(json, JsonOpts);
            if (parsed is not null)
                return parsed;
        }
        catch
        {
            // Intentamos un parseo más tolerante abajo.
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ProblemDetails(
                Status: TryGetInt(root, "status") ?? status,
                Title: TryGetString(root, "title") ?? "Error",
                Detail: TryGetString(root, "detail")
                    ?? TryGetString(root, "message")
                    ?? FirstValidationError(root)
                    ?? $"La API devolvió HTTP {status}.",
                Instance: TryGetString(root, "instance") ?? string.Empty,
                Details: TryGetValidationErrors(root),
                RetryAfterSeconds: TryGetInt(root, "retryAfterSeconds"));
        }
        catch
        {
            return new ProblemDetails(
                Status: status,
                Title: "Error",
                Detail: json,
                Instance: string.Empty);
        }
    }

    private static Dictionary<string, string[]>? TryGetValidationErrors(JsonElement root)
    {
        foreach (var propertyName in new[] { "details", "errors" })
        {
            if (!root.TryGetProperty(propertyName, out var value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;

                var errors = property.Value
                    .EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .ToArray();

                if (errors.Length > 0)
                    result[property.Name] = errors;
            }

            if (result.Count > 0)
                return result;
        }

        return null;
    }

    private static string? FirstValidationError(JsonElement root)
        => TryGetValidationErrors(root)?
            .SelectMany(kvp => kvp.Value)
            .FirstOrDefault();

    private static string? TryGetString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? TryGetInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : null;

    private static ProblemDetails FallbackProblem(int status) => new(
        Status: status,
        Title: "Error",
        Detail: $"La API devolvió HTTP {status}.",
        Instance: string.Empty
    );

    private sealed record RefreshTokenRequest(string RefreshToken);
}
