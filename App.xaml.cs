using Microsoft.Extensions.DependencyInjection;
using Paynest.Features.Auth.Login;
using Paynest.Features.Splash;
using Paynest.Services;

namespace Paynest;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var authState = MauiProgram.Services.GetRequiredService<AuthStateService>();

        // Cuando la sesión se cierre desde cualquier parte, volver al login
        authState.SessionChanged += () =>
        {
            if (!authState.IsAuthenticated)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    Current!.MainPage = BuildLoginNavigationPage());
            }
        };

        // Siempre arranca con el splash animado; él decide a dónde navegar
        var splash = MauiProgram.Services.GetRequiredService<SplashPage>();
        return new Window(splash);
    }

    private static NavigationPage BuildLoginNavigationPage()
    {
        var loginPage = MauiProgram.Services.GetRequiredService<LoginPage>();
        return new NavigationPage(loginPage)
        {
            BarBackgroundColor = Colors.Transparent,
            BackgroundColor    = Color.FromArgb("#F2F0EB")
        };
    }
}
