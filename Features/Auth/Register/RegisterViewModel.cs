using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Auth.Register;

public partial class RegisterViewModel(
    AuthStateService authState,
    ILogger<RegisterViewModel> logger) : ObservableObject
{
    // ── campos del formulario ────────────────────────────────────────────────

    [ObservableProperty] private string _fullName        = string.Empty;
    [ObservableProperty] private string _email           = string.Empty;
    [ObservableProperty] private string _password        = string.Empty;
    [ObservableProperty] private string _passwordConfirm = string.Empty;

    // ── estado de errores ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFullNameError))]
    private string? _fullNameError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailError))]
    private string? _emailError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordError))]
    private string? _passwordError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordConfirmError))]
    private string? _passwordConfirmError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeneralError))]
    private string? _generalError;

    public bool HasFullNameError        => FullNameError        is not null;
    public bool HasEmailError           => EmailError           is not null;
    public bool HasPasswordError        => PasswordError        is not null;
    public bool HasPasswordConfirmError => PasswordConfirmError is not null;
    public bool HasGeneralError         => GeneralError         is not null;

    // ── estado de carga ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    // ── toggle contraseñas ───────────────────────────────────────────────────

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

    // ── comandos ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task RegisterAsync()
    {
        if (!Validate()) return;

        IsLoading    = true;
        GeneralError = null;

        try
        {
            await authState.RegisterAsync(
                new RegisterRequest(Email.Trim(), FullName.Trim(), Password, PasswordConfirm));
            NavigateToApp();
        }
        catch (AuthException ex)
        {
            logger.LogWarning(ex, "Register failed with API problem response");
            HandleApiError(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Register failed before a usable API error was produced");
#if DEBUG
            GeneralError = $"Error de conexión: {ex.Message}";
#else
            GeneralError = "Sin conexión. Verifica tu internet e intenta de nuevo.";
#endif
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task GoToLoginAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private bool Validate()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullNameError = "Ingresa tu nombre completo";
            ok = false;
        }
        else FullNameError = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Ingresa tu correo";
            ok = false;
        }
        else if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EmailError = "Ingresa un correo válido";
            ok = false;
        }
        else EmailError = null;

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordError = "Ingresa una contraseña";
            ok = false;
        }
        else if (Password.Length < 12)
        {
            PasswordError = "Mínimo 12 caracteres";
            ok = false;
        }
        else if (!Regex.IsMatch(Password, @"[A-Z]"))
        {
            PasswordError = "Incluye al menos una mayúscula";
            ok = false;
        }
        else if (!Regex.IsMatch(Password, @"[a-z]"))
        {
            PasswordError = "Incluye al menos una minúscula";
            ok = false;
        }
        else if (!Regex.IsMatch(Password, @"\d"))
        {
            PasswordError = "Incluye al menos un número";
            ok = false;
        }
        else if (!Regex.IsMatch(Password, @"[^A-Za-z0-9]"))
        {
            PasswordError = "Incluye al menos un carácter especial";
            ok = false;
        }
        else PasswordError = null;

        if (string.IsNullOrWhiteSpace(PasswordConfirm))
        {
            PasswordConfirmError = "Confirma tu contraseña";
            ok = false;
        }
        else if (Password != PasswordConfirm)
        {
            PasswordConfirmError = "Las contraseñas no coinciden";
            ok = false;
        }
        else PasswordConfirmError = null;

        return ok;
    }

    private void HandleApiError(AuthException ex)
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
            if (p.Details.TryGetValue("email",           out var em)) EmailError           = em[0];
            if (p.Details.TryGetValue("fullName",        out var fn)) FullNameError        = fn[0];
            if (p.Details.TryGetValue("password",        out var pw)) PasswordError        = pw[0];
            if (p.Details.TryGetValue("passwordConfirm", out var pc)) PasswordConfirmError = pc[0];
            return;
        }

        GeneralError = p.Detail;
    }

    private static void NavigateToApp()
    {
        var shell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current!.MainPage = shell;
    }
}
