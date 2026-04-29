using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Models.Auth;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Auth.Login;

public partial class LoginViewModel(AuthStateService authState, IServiceProvider sp)
    : ObservableObject
{
    // ── campos del formulario ────────────────────────────────────────────────

    [ObservableProperty] private string _email    = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    // ── estado de errores ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailError))]
    private string? _emailError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordError))]
    private string? _passwordError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeneralError))]
    private string? _generalError;

    public bool HasEmailError    => EmailError    is not null;
    public bool HasPasswordError => PasswordError is not null;
    public bool HasGeneralError  => GeneralError  is not null;

    // ── estado de carga ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    // ── toggle contraseña ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeIconSource))]
    private bool _isPasswordHidden = true;

    public string EyeIconSource => IsPasswordHidden ? "ic_eye_off.svg" : "ic_eye.svg";

    [RelayCommand]
    void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    // ── comandos ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task LoginAsync()
    {
        if (!Validate()) return;

        IsLoading     = true;
        EmailError    = null;
        PasswordError = null;
        GeneralError  = null;

        try
        {
            await authState.LoginAsync(new LoginRequest(Email.Trim(), Password));
            NavigateToApp();
        }
        catch (AuthException ex)
        {
            HandleApiError(ex);
        }
        catch
        {
            GeneralError = "Sin conexión. Verifica tu internet e intenta de nuevo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task GoToRegisterAsync()
    {
        var page = sp.GetRequiredService<Features.Auth.Register.RegisterPage>();
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    void ForgotPassword()
    {
        // TODO: implementar flujo recuperar contraseña
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private bool Validate()
    {
        var ok = true;

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
            PasswordError = "Ingresa tu contraseña";
            ok = false;
        }
        else PasswordError = null;

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

        if (p.Status == 401)
        {
            GeneralError = "Credenciales inválidas.";
            return;
        }

        if (p.Details is { Count: > 0 })
        {
            if (p.Details.TryGetValue("email",    out var em)) EmailError    = em[0];
            if (p.Details.TryGetValue("password", out var pw)) PasswordError = pw[0];
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
