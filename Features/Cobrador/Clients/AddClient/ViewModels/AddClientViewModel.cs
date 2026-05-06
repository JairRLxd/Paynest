using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Clients.AddClient.ViewModels;

public partial class AddClientViewModel : ObservableObject
{
    private readonly AuthStateService _authState;
    private readonly ICollectorInviteService _collectorInviteService;
    private byte[]? _qrBytes;

    public AddClientViewModel(
        AuthStateService authState,
        ICollectorInviteService collectorInviteService)
    {
        _authState = authState;
        _collectorInviteService = collectorInviteService;

        var collectorId = _authState.CurrentUser?.Id ?? "anonymous_collector";
        CollectorCode = _collectorInviteService.GetOrCreateCollectorCode(collectorId);
        RefreshQrCode();
    }

    [ObservableProperty] private string _collectorCode = string.Empty;
    [ObservableProperty] private ImageSource? _qrCodeImage;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phoneNumber = string.Empty;
    [ObservableProperty] private string _amount = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    [RelayCommand]
    async Task BackAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.Navigation.PopAsync();
            return;
        }

        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
            await page.Navigation.PopAsync();
    }

    [RelayCommand]
    async Task CopyCodeAsync()
    {
        await Clipboard.Default.SetTextAsync(CollectorCode);
        await ShowMessageAsync("Código copiado", "Tu código de cobrador ya está en el portapapeles.");
    }

    [RelayCommand]
    Task CreateClientAsync() => ShowPendingAlertAsync("Registrar cliente");

    partial void OnCollectorCodeChanged(string value) => RefreshQrCode();

    private void RefreshQrCode()
    {
        if (string.IsNullOrWhiteSpace(CollectorCode))
        {
            QrCodeImage = null;
            _qrBytes = null;
            return;
        }

        _qrBytes = _collectorInviteService.GenerateQrPng(CollectorCode);
        QrCodeImage = ImageSource.FromStream(() => new MemoryStream(_qrBytes, writable: false));
    }

    private static async Task ShowPendingAlertAsync(string action)
    {
        await ShowMessageAsync(
            action,
            "Esta pantalla ya está lista. La funcionalidad se conectará en el siguiente paso.");
    }

    private static async Task ShowMessageAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return;

        await page.DisplayAlertAsync(title, message, "Entendido");
    }
}
