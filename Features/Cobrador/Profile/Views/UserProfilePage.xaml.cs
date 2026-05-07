namespace Paynest.Features.Cobrador.Profile.Views;

public partial class UserProfilePage : ContentPage
{
    private readonly Services.AuthStateService _authState;

    public UserProfilePage()
    {
        InitializeComponent();
        _authState = ServiceHelper.GetService<Services.AuthStateService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var user = _authState.CurrentUser;
        UserNameLabel.Text = user is null
            ? "Cobrador"
            : $"{user.FirstName} {user.LastNameP}".Trim();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Cerrar sesión",
            "¿Seguro que quieres salir de tu cuenta?",
            "Sí, salir",
            "Cancelar");

        if (!confirm)
            return;

        await _authState.LogoutAsync();
    }
}
