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
        NavigateAfterSplash();
    }

    private async Task PlayAnimation()
    {
        // Fase 1: Logo entra — fade-in + scale 0.82 → 1.0, ease-out
        await Task.WhenAll(
            LogoImage.FadeTo(1, 450, Easing.CubicOut),
            LogoImage.ScaleTo(1.0, 450, Easing.CubicOut)
        );

        // Fase 2: Micro-bounce profesional
        await LogoImage.ScaleTo(1.06, 110, Easing.CubicOut);
        await LogoImage.ScaleTo(1.0,  140, Easing.CubicIn);

        // Fase 3: Hold
        await Task.Delay(1100);
    }

    private void NavigateAfterSplash()
    {
        Page next = _authState.IsAuthenticated
            ? MauiProgram.Services.GetRequiredService<AppShell>()
            : BuildLoginPage();

        Application.Current!.MainPage = next;
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
