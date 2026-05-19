using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Features.Onboarding.CompleteProfile;
using Paynest.Features.Onboarding.IdentityVerification;
using Paynest.Features.Onboarding.PaymentSetup;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class UserProfilePage : ContentPage
{
    private readonly AuthStateService _authState;
    private readonly IProfileService _profileService;
    private readonly HttpClient _httpClient;
    private bool _selfieLoaded;

    public UserProfilePage()
    {
        InitializeComponent();
        _authState   = ServiceHelper.GetService<AuthStateService>();
        _profileService = ServiceHelper.GetService<IProfileService>();
        _httpClient  = ServiceHelper.GetService<HttpClient>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PopulateUser();
        await LoadSelfieAvatarAsync();
    }

    private void PopulateUser()
    {
        var user = _authState.CurrentUser;
        if (user is null) return;

        var firstName  = user.FirstName.Trim();
        var lastNameP  = user.LastNameP.Trim();
        var lastNameM  = user.LastNameM?.Trim() ?? string.Empty;
        var initials   = $"{(firstName.Length > 0 ? firstName[0] : '?')}{(lastNameP.Length > 0 ? lastNameP[0] : '?')}";
        var fullName   = string.Join(" ", new[] { firstName, lastNameP, lastNameM }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));

        InitialsLabel.Text = initials.ToUpper();
        FullNameLabel.Text = fullName;
        EmailLabel.Text    = user.Email;
        EmailRowLabel.Text = user.Email;

        var roleDisplay = _authState.NormalizedRole switch
        {
            "admin_collector" => "Admin Cobrador",
            "collector"       => "Cobrador",
            "client"          => "Cliente",
            _                 => user.Role
        };
        RoleLabel.Text    = roleDisplay.ToUpper();
        RoleRowLabel.Text = roleDisplay;
    }

    private async Task LoadSelfieAvatarAsync()
    {
        if (_selfieLoaded) return;
        try
        {
            var docs   = await _profileService.GetDocumentsStatusAsync();
            var selfie = docs.Documents.FirstOrDefault(d => d.Type == "Selfie");
            var url    = selfie?.Url;

            if (string.IsNullOrWhiteSpace(url)) return;

            var token = _authState.AccessToken;
            if (string.IsNullOrWhiteSpace(token)) return;

            var separator    = url.Contains('?') ? "&" : "?";
            var urlWithToken = $"{url}{separator}token={Uri.EscapeDataString(token)}";

            using var req  = new HttpRequestMessage(HttpMethod.Get, urlWithToken);
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return;

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            SelfieImage.Source           = ImageSource.FromStream(() => new MemoryStream(bytes, writable: false));
            SelfieAvatarBorder.IsVisible = true;
            _selfieLoaded                = true;
        }
        catch
        {
            // Si falla la carga de la foto no rompemos la pantalla; se muestran las iniciales
        }
    }

    private async void OnAvatarTapped(object? sender, TappedEventArgs e)
    {
        // Lleva directamente a la pantalla de documentos en modo edición
        // para que el usuario pueda actualizar su selfie
        _selfieLoaded = false;
        var page = MauiProgram.Services.GetRequiredService<IdentityVerificationPage>();
        if (page.BindingContext is IdentityVerificationViewModel vm)
            vm.IsEditMode = true;
        await Navigation.PushAsync(page);
    }

    private async void OnEditOnboardingTapped(object? sender, TappedEventArgs e)
    {
        var page = MauiProgram.Services.GetRequiredService<CompleteProfilePage>();
        if (page.BindingContext is CompleteProfileViewModel vm)
            vm.IsEditMode = true;
        await Navigation.PushAsync(page);
    }

    private async void OnEditIdentityTapped(object? sender, TappedEventArgs e)
    {
        _selfieLoaded = false;
        var page = MauiProgram.Services.GetRequiredService<IdentityVerificationPage>();
        if (page.BindingContext is IdentityVerificationViewModel vm)
            vm.IsEditMode = true;
        await Navigation.PushAsync(page);
    }

    private async void OnEditPaymentTapped(object? sender, TappedEventArgs e)
    {
        var page = MauiProgram.Services.GetRequiredService<PaymentSetupPage>();
        if (page.BindingContext is PaymentSetupViewModel vm)
            vm.IsEditMode = true;
        await Navigation.PushAsync(page);
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Cerrar sesión",
            "¿Seguro que quieres salir de tu cuenta?",
            "Sí, salir",
            "Cancelar");

        if (!confirm) return;

        await _authState.LogoutAsync();
    }
}
