using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Features.Cobrador.Clients.Models;

namespace Paynest.Features.Cobrador.Clients.ComprobanteViewer.ViewModels;

public partial class ComprobanteViewerViewModel : ObservableObject
{
    [ObservableProperty] private string _clientNameUpper = string.Empty;
    [ObservableProperty] private string _debtTitle       = string.Empty;
    [ObservableProperty] private string _amount          = string.Empty;
    [ObservableProperty] private string _statusLabel     = string.Empty;
    [ObservableProperty] private Color  _statusColor     = Colors.Gray;
    [ObservableProperty] private Color  _statusBadgeBg   = Color.FromArgb("#F3F4F6");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProof), nameof(HasNoProof), nameof(ProofImageSource))]
    private string? _proofImagePath;

    public bool HasProof   => ProofImagePath is not null;
    public bool HasNoProof => ProofImagePath is null;
    public ImageSource? ProofImageSource
        => ProofImagePath is not null ? ImageSource.FromFile(ProofImagePath) : null;

    public Action? OnApproved { get; set; }
    public Action? OnRejected { get; set; }

    public void Load(ClientDebtItem debt, string clientName)
    {
        ClientNameUpper = clientName.ToUpperInvariant();
        DebtTitle       = debt.Title;
        Amount          = debt.Amount;
        StatusLabel     = debt.Status;
        StatusColor     = debt.StatusTextColor;
        StatusBadgeBg   = debt.StatusBackground;
        ProofImagePath  = debt.ProofImagePath;
        OnApproved      = null;
        OnRejected      = null;
    }

    [RelayCommand]
    async Task BackAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.Navigation.PopAsync();
    }

    [RelayCommand]
    async Task ApproveAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        bool ok = await page.DisplayAlertAsync(
            "Aprobar pago",
            "¿Confirmas que el comprobante es válido y el pago es correcto?",
            "Aprobar",
            "Cancelar");

        if (!ok) return;

        await page.Navigation.PopAsync();
        OnApproved?.Invoke();
    }

    [RelayCommand]
    async Task RejectAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        bool ok = await page.DisplayAlertAsync(
            "Rechazar comprobante",
            "¿Seguro que quieres rechazar este comprobante? El pago quedará como pendiente.",
            "Rechazar",
            "Cancelar");

        if (!ok) return;

        await page.Navigation.PopAsync();
        OnRejected?.Invoke();
    }
}
