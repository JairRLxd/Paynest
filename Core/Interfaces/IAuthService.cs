#nullable enable
using Paynest.Core.Models.Auth;

namespace Paynest.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string? refreshToken = null, CancellationToken ct = default);
    Task<UserResponse> MeAsync(string accessToken, CancellationToken ct = default);
}
