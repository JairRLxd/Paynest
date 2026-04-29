using System.Net;
using Microsoft.Extensions.Logging;
using Paynest.Core.Interfaces;
using Paynest.Features.Auth.Login;
using Paynest.Features.Auth.Register;
using Paynest.Features.Splash;
using Paynest.Infrastructure;
using Paynest.Infrastructure.Http;
using Paynest.Services;

namespace Paynest;

public static class MauiProgram
{
    // Acceso estático al service provider para navegación desde ViewModels
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",   "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf",  "OpenSansSemibold");
            });

        // ── HTTP client con cookie container compartido ──────────────────────
        // El CookieContainer persiste la cookie HttpOnly del refresh token
        // mientras la app esté en memoria. Si el proceso se termina, el usuario
        // deberá volver a iniciar sesión (comportamiento esperado y seguro).
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler { CookieContainer = cookieContainer };
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiConstants.BaseUrl)
        };

        builder.Services.AddSingleton(httpClient);
        builder.Services.AddSingleton<IAuthService, AuthApiClient>();

        // ── Estado de autenticación (singleton en toda la app) ───────────────
        builder.Services.AddSingleton<AuthStateService>();

        // ── Shell ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<AppShell>();

        // ── Splash ───────────────────────────────────────────────────────────
        builder.Services.AddTransient<SplashPage>();

        // ── Módulo Auth ──────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
