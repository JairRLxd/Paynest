namespace Paynest.Core.Models.Auth;

public record ResetPasswordRequest(
    string Email,
    string Code,
    string NewPassword,
    string NewPasswordConfirm);
