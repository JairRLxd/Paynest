using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Services;

/// <summary>
/// Singleton que guarda el accessToken en memoria y gestiona el flujo de sesión.
/// El refresh token vive en la cookie HttpOnly del handler; nunca se toca desde aquí.
/// </summary>
public class AuthStateService(IAuthService authService)
{
    private readonly IAuthService _auth = authService;

    public UserResponse? CurrentUser  { get; private set; }
    public string?       AccessToken  { get; private set; }
    public bool          IsAuthenticated => AccessToken is not null;

    public event Action? SessionChanged;

    // ── operaciones de sesión ────────────────────────────────────────────────

    public async Task LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _auth.LoginAsync(request, ct);
        SetSession(response);
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await _auth.RegisterAsync(request, ct);
        SetSession(response);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try { await _auth.LogoutAsync(ct); } catch { /* continuar aunque falle la API */ }
        ClearSession();
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
                refreshed = await _auth.RefreshAsync(ct);
            }
            catch
            {
                ClearSession();
                throw;
            }

            SetSession(refreshed);
            return await call(AccessToken!);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void SetSession(AuthResponse response)
    {
        AccessToken  = response.AccessToken;
        CurrentUser  = response.User;
        SessionChanged?.Invoke();
    }

    private void ClearSession()
    {
        AccessToken  = null;
        CurrentUser  = null;
        SessionChanged?.Invoke();
    }
}
