#nullable enable
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Auth;
using Paynest.Core.Validation;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Auth.ForgotPassword;

public partial class ForgotPasswordViewModel(IAuthService authService) : ObservableObject
{
    // ── Paso 1: email ────────────────────────────────────────────────────────

    [ObservableProperty] private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailError))]
    private string? _emailError;

    public bool HasEmailError => EmailError is not null;

    // ── Paso 2: código + nueva contraseña ────────────────────────────────────

    [ObservableProperty] private string _code            = string.Empty;
    [ObservableProperty] private string _newPassword     = string.Empty;
    [ObservableProperty] private string _passwordConfirm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCodeError))]
    private string? _codeError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNewPasswordError))]
    private string? _newPasswordError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordConfirmError))]
    private string? _passwordConfirmError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeneralError))]
    private string? _generalError;

    public bool HasCodeError            => CodeError           is not null;
    public bool HasNewPasswordError     => NewPasswordError    is not null;
    public bool HasPasswordConfirmError => PasswordConfirmError is not null;
    public bool HasGeneralError         => GeneralError        is not null;

    // ── Toggle visibilidad contraseñas ───────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeIconSource))]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeConfirmIconSource))]
    private bool _isPasswordConfirmHidden = true;

    public string EyeIconSource        => IsPasswordHidden        ? "ic_eye_off.svg" : "ic_eye.svg";
    public string EyeConfirmIconSource => IsPasswordConfirmHidden ? "ic_eye_off.svg" : "ic_eye.svg";

    [RelayCommand] void TogglePassword()        => IsPasswordHidden        = !IsPasswordHidden;
    [RelayCommand] void TogglePasswordConfirm() => IsPasswordConfirmHidden = !IsPasswordConfirmHidden;

    // ── Estado ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading), nameof(IsStep1), nameof(IsStep2))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    // Paso 1 = solicitar código; Paso 2 = ingresar código + nueva contraseña
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2))]
    private int _step = 1;

    public bool IsStep1 => Step == 1 && !IsLoading;
    public bool IsStep2 => Step == 2 && !IsLoading;

    // Mensaje de éxito final para mostrar antes de navegar
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    public bool HasSuccess => SuccessMessage is not null;

    // ── Comandos ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task RequestCodeAsync()
    {
        if (!ValidateStep1()) return;

        IsLoading    = true;
        GeneralError = null;
        try
        {
            await authService.ForgotPasswordAsync(
                new ForgotPasswordRequest(InputSanitizer.Email(Email)));
            Step = 2;
        }
        catch (AuthException ex)
        {
            HandleApiError(ex, isStep1: true);
        }
        catch (Exception ex)
        {
            GeneralError = NetworkErrorMessageProvider.From(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task ResetPasswordAsync()
    {
        if (!ValidateStep2()) return;

        IsLoading    = true;
        GeneralError = null;
        try
        {
            await authService.ResetPasswordAsync(new ResetPasswordRequest(
                InputSanitizer.Email(Email),
                Code.Trim(),
                NewPassword,
                PasswordConfirm));

            SuccessMessage = "Contraseña actualizada. Ya puedes iniciar sesión.";
        }
        catch (AuthException ex)
        {
            HandleApiError(ex, isStep1: false);
        }
        catch (Exception ex)
        {
            GeneralError = NetworkErrorMessageProvider.From(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Validación ───────────────────────────────────────────────────────────

    private bool ValidateStep1()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Ingresa tu correo";
            return false;
        }
        if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EmailError = "Ingresa un correo válido";
            return false;
        }
        EmailError = null;
        return true;
    }

    private bool ValidateStep2()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(Code) || Code.Trim().Length < 4)
        {
            CodeError = "Ingresa el código que recibiste";
            ok = false;
        }
        else CodeError = null;

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            NewPasswordError = "Ingresa tu nueva contraseña";
            ok = false;
        }
        else if (NewPassword.Length < 12)
        {
            NewPasswordError = "Mínimo 12 caracteres";
            ok = false;
        }
        else if (!Regex.IsMatch(NewPassword, @"[A-Z]"))
        {
            NewPasswordError = "Incluye al menos una mayúscula";
            ok = false;
        }
        else if (!Regex.IsMatch(NewPassword, @"[a-z]"))
        {
            NewPasswordError = "Incluye al menos una minúscula";
            ok = false;
        }
        else if (!Regex.IsMatch(NewPassword, @"\d"))
        {
            NewPasswordError = "Incluye al menos un número";
            ok = false;
        }
        else if (!Regex.IsMatch(NewPassword, @"[^A-Za-z0-9]"))
        {
            NewPasswordError = "Incluye al menos un carácter especial";
            ok = false;
        }
        else NewPasswordError = null;

        if (string.IsNullOrWhiteSpace(PasswordConfirm))
        {
            PasswordConfirmError = "Confirma tu contraseña";
            ok = false;
        }
        else if (NewPassword != PasswordConfirm)
        {
            PasswordConfirmError = "Las contraseñas no coinciden";
            ok = false;
        }
        else PasswordConfirmError = null;

        return ok;
    }

    private void HandleApiError(AuthException ex, bool isStep1)
    {
        var p = ex.Problem;

        if (p.Status == 429)
        {
            var secs = p.RetryAfterSeconds ?? 60;
            GeneralError = $"Demasiados intentos. Espera {secs} segundos.";
            return;
        }

        if (p.Details is { Count: > 0 })
        {
            if (isStep1)
            {
                if (p.Details.TryGetValue("email", out var em)) EmailError = em[0];
            }
            else
            {
                if (p.Details.TryGetValue("code",            out var co)) CodeError           = co[0];
                if (p.Details.TryGetValue("newPassword",     out var np)) NewPasswordError    = np[0];
                if (p.Details.TryGetValue("passwordConfirm", out var pc)) PasswordConfirmError = pc[0];
            }
            return;
        }

        GeneralError = p.Detail;
    }

    // Limpiar errores al editar
    partial void OnEmailChanged(string value)           { if (EmailError           is not null) EmailError           = null; }
    partial void OnCodeChanged(string value)            { if (CodeError            is not null) CodeError            = null; }
    partial void OnNewPasswordChanged(string value)     { if (NewPasswordError     is not null) NewPasswordError     = null; }
    partial void OnPasswordConfirmChanged(string value) { if (PasswordConfirmError is not null) PasswordConfirmError = null; }
}
