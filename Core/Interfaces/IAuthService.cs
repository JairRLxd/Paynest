using Paynest.Core.Models.Auth;

namespace Paynest.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserResponse> MeAsync(string accessToken, CancellationToken ct = default);
}
