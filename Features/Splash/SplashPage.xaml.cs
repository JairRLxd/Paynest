using Microsoft.Extensions.DependencyInjection;
using Paynest.Features.Auth.Login;
using Paynest.Services;

namespace Paynest.Features.Splash;

public partial class SplashPage : ContentPage
{
    private readonly AuthStateService _authState;

    public SplashPage(AuthStateService authState)
    {
        _authState = authState;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PlayAnimation();
        await NavigateAfterSplashAsync();
    }

    private async Task PlayAnimation()
    {
        // Fase 1: Logo entra — fade-in + scale 0.82 → 1.0, ease-out
        await Task.WhenAll(
            LogoImage.FadeToAsync(1, 450, Easing.CubicOut),
            LogoImage.ScaleToAsync(1.0, 450, Easing.CubicOut)
        );

        // Fase 2: Micro-bounce profesional
        await LogoImage.ScaleToAsync(1.06, 110, Easing.CubicOut);
        await LogoImage.ScaleToAsync(1.0,  140, Easing.CubicIn);

        // Fase 3: Hold
        await Task.Delay(1100);
    }

    private async Task NavigateAfterSplashAsync()
    {
        var hasSession = await _authState.RestoreSessionAsync();
        Page next = hasSession
            ? App.BuildAuthenticatedRootPage(_authState)
            : BuildLoginPage();

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = next;
        }
    }

    private static NavigationPage BuildLoginPage()
    {
        var page = MauiProgram.Services.GetRequiredService<LoginPage>();
        return new NavigationPage(page)
        {
            BarBackgroundColor = Colors.Transparent,
            BackgroundColor    = Color.FromArgb("#F2F0EB")
        };
    }
}
