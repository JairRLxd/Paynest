#nullable enable
using System.Text.Json;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Services;

/// <summary>
/// Singleton que guarda accessToken en memoria, lo persiste de forma segura y gestiona el flujo de sesión.
/// </summary>
public class AuthStateService(IAuthService authService)
{
    private const string AccessTokenKey = "paynest.auth.access_token";
    private const string RefreshTokenKey = "paynest.auth.refresh_token";
    private const string UserKey = "paynest.auth.user";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAuthService _auth = authService;

    public UserResponse? CurrentUser  { get; private set; }
    public string?       AccessToken  { get; private set; }
    public string?       RefreshToken { get; private set; }
    public bool          IsAuthenticated => AccessToken is not null;

    // Lee profileCompleted directo de la API.
    // MarkProfileCompleted() se llama al terminar el onboarding para refrescar el token.
    public bool IsProfileCompleted => CurrentUser?.ProfileCompleted ?? false;

    public string NormalizedRole => CurrentUser?.Role?.Trim().ToLowerInvariant() ?? string.Empty;
    public bool IsClient => NormalizedRole == "client";
    public bool IsCollectorRole => NormalizedRole is "collector" or "admin" or "admin_collector";
    public bool IsAdminCollector => NormalizedRole == "admin_collector";
    public bool RequiresCollectorOnboarding => IsAdminCollector && !IsProfileCompleted;

    public void MarkProfileCompleted()
    {
        if (CurrentUser is null) return;
        // Optimistic update local hasta que el próximo login refresque CurrentUser
        CurrentUser = CurrentUser with { ProfileCompleted = true };
        Preferences.Default.Set($"profile_done_{CurrentUser.Id}", true);
    }

    public event Action? SessionChanged;

    // ── operaciones de sesión ────────────────────────────────────────────────

    public async Task LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _auth.LoginAsync(request, ct);
        await SetSessionAsync(response, persist: true, ct);
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await _auth.RegisterAsync(request, ct);
        await SetSessionAsync(response, persist: true, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try { await _auth.LogoutAsync(RefreshToken, ct); } catch { /* continuar aunque falle la API */ }
        await ClearSessionAsync();
    }

    public async Task<bool> RestoreSessionAsync(CancellationToken ct = default)
    {
        if (IsAuthenticated)
        {
            return true;
        }

        try
        {
            var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            var userJson = await SecureStorage.Default.GetAsync(UserKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var user = string.IsNullOrWhiteSpace(userJson)
                ? null
                : JsonSerializer.Deserialize<UserResponse>(userJson, JsonOpts);

            AccessToken = token;
            RefreshToken = refreshToken;
            CurrentUser = user;

            var verifiedUser = await _auth.MeAsync(token, ct);
            CurrentUser = verifiedUser;
            await PersistSessionAsync(token, refreshToken, verifiedUser);
            SessionChanged?.Invoke();
            return true;
        }
        catch (AuthException ex) when (ex.Problem.Status == 401)
        {
            try
            {
                var refreshToken = RefreshToken ?? await SecureStorage.Default.GetAsync(RefreshTokenKey);
                if (string.IsNullOrWhiteSpace(refreshToken))
                    throw new InvalidOperationException("No hay refresh token persistido.");

                var refreshed = await _auth.RefreshAsync(refreshToken, ct);
                await SetSessionAsync(refreshed, persist: true, ct);
                return true;
            }
            catch
            {
                await ClearSessionAsync();
                return false;
            }
        }
        catch
        {
            await ClearSessionAsync();
            return false;
        }
    }

    // ── request protegida con retry automático ───────────────────────────────

    /// <summary>
    /// Ejecuta <paramref name="call"/> con el accessToken vigente.
    /// Si recibe 401, intenta refresh una vez; si vuelve a fallar, cierra la sesión.
    /// </summary>
    public async Task<T> CallProtectedAsync<T>(Func<string, Task<T>> call,
                                               CancellationToken ct = default)
    {
        if (AccessToken is null)
            throw new InvalidOperationException("No hay sesión activa.");

        try
        {
            return await call(AccessToken);
        }
        catch (AuthException ex) when (ex.Problem.Status == 401)
        {
            // Intentar un refresh
            AuthResponse refreshed;
            try
            {
                if (string.IsNullOrWhiteSpace(RefreshToken))
                    throw new InvalidOperationException("No hay refresh token persistido.");

                refreshed = await _auth.RefreshAsync(RefreshToken, ct);
            }
            catch
            {
                await ClearSessionAsync();
                throw;
            }

            await SetSessionAsync(refreshed, persist: true, ct);
            return await call(AccessToken!);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task SetSessionAsync(AuthResponse response, bool persist, CancellationToken ct)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        CurrentUser = response.User ?? new UserResponse(
            response.Id,
            response.Email,
            response.FirstName,
            response.LastNameP,
            response.LastNameM,
            response.EmailVerified,
            response.Role,
            response.ProfileCompleted);

        if (persist)
        {
            await PersistSessionAsync(response.AccessToken, response.RefreshToken, CurrentUser);
        }

        ct.ThrowIfCancellationRequested();
        SessionChanged?.Invoke();
    }

    private async Task PersistSessionAsync(string accessToken, string? refreshToken, UserResponse user)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(user, JsonOpts));
    }

    private async Task ClearSessionAsync()
    {
        AccessToken  = null;
        RefreshToken = null;
        CurrentUser  = null;
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserKey);
        await Task.CompletedTask;
        SessionChanged?.Invoke();
    }
}
