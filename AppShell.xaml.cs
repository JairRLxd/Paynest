using Paynest.Features.Cobrador.Pages;
using Paynest.Services;
#if ANDROID
using Android.Widget;
#endif

namespace Paynest;

public partial class AppShell : Shell
{
    private readonly IClientDebtService _clientDebtService;
    private readonly AuthStateService _authState;

    public AppShell()
    {
        InitializeComponent();
        _clientDebtService = ServiceHelper.GetService<IClientDebtService>();
        _authState = ServiceHelper.GetService<AuthStateService>();

        RegisterRoutes();
        ConfigureRoleShell();

        _clientDebtService.CurrentGroupChanged += OnCurrentGroupChanged;
        _authState.SessionChanged += OnSessionChanged;
        RefreshFlyoutHeader();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(DebtDetailPage), typeof(DebtDetailPage));
        Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
        Routing.RegisterRoute(nameof(LinkCollectorPage), typeof(LinkCollectorPage));
        Routing.RegisterRoute(nameof(AddClientPage), typeof(AddClientPage));
        Routing.RegisterRoute(nameof(ClientPickerPage), typeof(ClientPickerPage));
        Routing.RegisterRoute(nameof(ClientDetailPage), typeof(ClientDetailPage));
        Routing.RegisterRoute(nameof(EditClientPage), typeof(EditClientPage));
        Routing.RegisterRoute(nameof(RegisterPaymentPage), typeof(RegisterPaymentPage));
        Routing.RegisterRoute(nameof(ComprobanteViewerPage), typeof(ComprobanteViewerPage));
        Routing.RegisterRoute(nameof(CreateDebtPage), typeof(CreateDebtPage));
        Routing.RegisterRoute(nameof(CalendarPickerPage), typeof(CalendarPickerPage));
    }

    private void ConfigureRoleShell()
    {
        if (_authState.IsClient)
        {
            Items.Remove(CollectorTabBar);
            FlyoutBehavior = FlyoutBehavior.Flyout;
            return;
        }

        Items.Remove(ClientTabBar);
        FlyoutBehavior = FlyoutBehavior.Disabled;
    }

    private async void OnSwitchGroupClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        var groups = _clientDebtService.GetGroups()
            .Where(g => g.PendingAmount > 0)
            .ToList();
        if (groups.Count == 0)
        {
            try
            {
                groups = (await _clientDebtService.GetGroupsAsync())
                    .Where(g => g.PendingAmount > 0)
                    .ToList();
            }
            catch
            {
                await DisplayAlertAsync("Sin conexión", "No pudimos cargar tus deudas. Intenta nuevamente.", "OK");
                return;
            }
        }

        if (groups.Count == 0)
        {
            await DisplayAlertAsync("Sin deudas", "No tienes deudas activas para seleccionar.", "OK");
            return;
        }

        var options = groups.Select(g => g.Name).ToArray();
        var selectedName = await Current.DisplayActionSheetAsync("Cambiar deuda activa", "Cancelar", null, options);

        if (string.IsNullOrEmpty(selectedName) || selectedName == "Cancelar")
        {
            return;
        }

        var selected = groups.FirstOrDefault(g => g.Name == selectedName);
        if (selected is null)
        {
            return;
        }

        _clientDebtService.SetCurrentGroup(selected.Id);
        RefreshFlyoutHeader();
        await DisplayAlertAsync("Deuda activa", $"Ahora estás viendo: {selected.Name}", "OK");
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Current.GoToAsync(nameof(NotificationsPage));
    }

    private async void OnLinkCollectorClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Current.GoToAsync(nameof(LinkCollectorPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        var confirm = await DisplayAlertAsync(
            "Cerrar sesión",
            "¿Seguro que quieres salir de tu cuenta?",
            "Sí, salir",
            "Cancelar");

        if (!confirm)
        {
            return;
        }

        await _authState.LogoutAsync();
        ShowLogoutFeedback();
    }

    private void OnCurrentGroupChanged(object? sender, EventArgs e)
    {
        RefreshFlyoutHeader();
    }

    private void OnSessionChanged()
    {
        RefreshFlyoutHeader();
    }

    private void RefreshFlyoutHeader()
    {
        FlyoutUserLabel.Text = _authState.CurrentUser?.FirstName ?? "Cliente";
        FlyoutGroupLabel.Text = _clientDebtService.CurrentGroup.Name;
    }

    private static void ShowLogoutFeedback()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Toast.MakeText(Android.App.Application.Context, "Sesion cerrada", ToastLength.Short)?.Show();
        });
#endif
    }
}
