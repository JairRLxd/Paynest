#nullable enable
using System.Text.RegularExpressions;
using Paynest.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paynest.Core.Models.Auth;
using Paynest.Features.Onboarding.CompleteProfile;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Auth.Register;

public partial class RegisterViewModel(
    AuthStateService authState,
    ILogger<RegisterViewModel> logger) : ObservableObject
{
    // ── campos del formulario ────────────────────────────────────────────────

    [ObservableProperty] private string _firstName       = string.Empty;
    [ObservableProperty] private string _lastNameP       = string.Empty;
    [ObservableProperty] private string _lastNameM       = string.Empty;
    [ObservableProperty] private string _email           = string.Empty;
    [ObservableProperty] private string _password        = string.Empty;
    [ObservableProperty] private string _passwordConfirm = string.Empty;

    // ── estado de errores ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFirstNameError))]
    private string? _firstNameError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastNamePError))]
    private string? _lastNamePError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastNameMError))]
    private string? _lastNameMError;

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

    public bool HasFirstNameError       => FirstNameError       is not null;
    public bool HasLastNamePError       => LastNamePError       is not null;
    public bool HasLastNameMError       => LastNameMError       is not null;
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
                new RegisterRequest(
                    InputSanitizer.Email(Email),
                    InputSanitizer.Text(FirstName),
                    InputSanitizer.Text(LastNameP),
                    InputSanitizer.Text(LastNameM),
                    Password,
                    PasswordConfirm));
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
            GeneralError = NetworkErrorMessageProvider.From(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task GoToLoginAsync()
    {
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation is not null)
        {
            await navigation.PopAsync();
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private bool Validate()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            FirstNameError = "Ingresa tu nombre";
            ok = false;
        }
        else FirstNameError = null;

        if (string.IsNullOrWhiteSpace(LastNameP))
        {
            LastNamePError = "Ingresa tu apellido paterno";
            ok = false;
        }
        else LastNamePError = null;

        if (string.IsNullOrWhiteSpace(LastNameM))
        {
            LastNameMError = "Ingresa tu apellido materno";
            ok = false;
        }
        else LastNameMError = null;

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
            if (p.Details.TryGetValue("firstName",       out var fn)) FirstNameError       = fn[0];
            if (p.Details.TryGetValue("lastNameP",       out var lp)) LastNamePError       = lp[0];
            if (p.Details.TryGetValue("lastNameM",       out var lm)) LastNameMError       = lm[0];
            if (p.Details.TryGetValue("password",        out var pw)) PasswordError        = pw[0];
            if (p.Details.TryGetValue("passwordConfirm", out var pc)) PasswordConfirmError = pc[0];
            return;
        }

        GeneralError = p.Detail;
    }

    private static void NavigateToApp()
    {
        var auth = MauiProgram.Services.GetRequiredService<AuthStateService>();
        App.SetRootPage(App.BuildAuthenticatedRootPage(auth));
    }

    partial void OnFirstNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(FirstNameError))
        {
            FirstNameError = null;
        }
    }

    partial void OnLastNamePChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(LastNamePError))
        {
            LastNamePError = null;
        }
    }

    partial void OnLastNameMChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(LastNameMError))
        {
            LastNameMError = null;
        }
    }

    partial void OnEmailChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(EmailError))
        {
            EmailError = null;
        }
    }

    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(PasswordError))
        {
            PasswordError = null;
        }
    }

    partial void OnPasswordConfirmChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(PasswordConfirmError))
        {
            PasswordConfirmError = null;
        }
    }
}
