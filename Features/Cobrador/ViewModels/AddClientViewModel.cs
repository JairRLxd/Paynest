using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Services;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class AddClientViewModel : ObservableObject
{
    private readonly AuthStateService _authState;
    private readonly ICollectorInviteService _collectorInviteService;
    private byte[]? _qrBytes;
    private string _qrPayload = string.Empty;

    public AddClientViewModel(
        AuthStateService authState,
        ICollectorInviteService collectorInviteService)
    {
        _authState = authState;
        _collectorInviteService = collectorInviteService;

        var collectorId = _authState.CurrentUser?.Id ?? "anonymous_collector";
        CollectorCode = _collectorInviteService.GetOrCreateCollectorCode(collectorId);
        InviteStatusText = "Preparando codigo...";
        RefreshQrCode();
    }

    [ObservableProperty] private string _collectorCode = string.Empty;
    [ObservableProperty] private ImageSource? _qrCodeImage;
    [ObservableProperty] private string _inviteStatusText = string.Empty;
    [ObservableProperty] private bool _isLoadingInvite;

    public async Task LoadInviteAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoadingInvite)
            return;

        try
        {
            IsLoadingInvite = true;
            var invite = await _collectorInviteService.GetInviteAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(invite.EffectiveCode))
            {
                CollectorCode = invite.EffectiveCode;
                _qrPayload = invite.EffectiveQrPayload;
                RefreshQrCode();
            }

            InviteStatusText = invite.IsLocalFallback
                ? "Codigo temporal local. Se sincronizara cuando backend exponga la invitacion."
                : "Codigo oficial listo para compartir.";
        }
        catch
        {
            InviteStatusText = "No pudimos actualizar el codigo. Puedes usar el codigo mostrado por ahora.";
        }
        finally
        {
            IsLoadingInvite = false;
        }
    }

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
    async Task ShareCodeAsync()
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Codigo de cobrador Paynest",
            Text = $"Usa este codigo para vincularte conmigo en Paynest: {CollectorCode}"
        });
    }

    partial void OnCollectorCodeChanged(string value) => RefreshQrCode();

    private void RefreshQrCode()
    {
        if (string.IsNullOrWhiteSpace(CollectorCode))
        {
            QrCodeImage = null;
            _qrBytes = null;
            return;
        }

        var payload = string.IsNullOrWhiteSpace(_qrPayload) ? CollectorCode : _qrPayload;
        _qrBytes = _collectorInviteService.GenerateQrPng(payload);
        QrCodeImage = ImageSource.FromStream(() => new MemoryStream(_qrBytes, writable: false));
    }

    private static async Task ShowMessageAsync(string title, string message)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return;

        await page.DisplayAlertAsync(title, message, "Entendido");
    }
}
