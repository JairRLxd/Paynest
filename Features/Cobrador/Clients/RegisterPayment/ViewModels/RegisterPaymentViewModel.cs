using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Features.Cobrador.Clients.Models;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Paynest.Features.Cobrador.Clients.RegisterPayment.ViewModels;

public partial class RegisterPaymentViewModel(
    ICollectorPaymentService paymentService,
    CollectorPaymentSettings paymentSettings) : ObservableObject
{
    private static readonly Color _chipSelected    = Color.FromArgb("#23935D");
    private static readonly Color _chipSoft        = Color.FromArgb("#E8F5EE");
    private static readonly Color _chipUnselStroke = Color.FromArgb("#DDE6DF");
    private static readonly Color _textDark        = Color.FromArgb("#34473A");

    private string _clientId = string.Empty;
    private string _debtId   = string.Empty;

    private PaymentPreviewResponse? _preview;
    private CancellationTokenSource _previewCts = new();
    private bool _suppressAmountPreview;

    // ── Encabezado ─────────────────────────────────────────────────────────────

    [ObservableProperty] private string _clientNameUpper  = string.Empty;
    [ObservableProperty] private string _totalDebt        = "—";
    [ObservableProperty] private string _currentDueAmount = "—";
    [ObservableProperty] private string _statusLabel      = "Pendiente";
    [ObservableProperty] private Color  _statusColor      = Color.FromArgb("#F79009");

    public Color StatusBadgeBg => StatusLabel switch
    {
        "Atrasado" or "Vencido" => Color.FromArgb("#FFF1F3"),
        "Al corriente" or "Al día" => Color.FromArgb("#ECFDF3"),
        _ => Color.FromArgb("#FFF3E0")
    };

    // ── Carga ──────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPaymentCommand))]
    private bool _isLoading;

    // ── Monto a pagar ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(HasCurrentDueBreakdown),
        nameof(CurrentDueBreakdownText))]
    private string _amountToPay = "0.00";

    partial void OnAmountToPayChanged(string value)
    {
        if (_suppressAmountPreview || !IsPartialPayment) return;
        TriggerPreviewDebounced();
    }

    // ── Tipo de pago ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsPartialPayment),
        nameof(IsPartialPaymentAllowed),
        nameof(TotalPaymentBg), nameof(PartialPaymentBg),
        nameof(TotalPaymentText), nameof(PartialPaymentText))]
    private bool _isTotalPayment = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPartialPaymentBlocked))]
    private bool _isPartialPaymentAllowed = true;

    public bool IsPartialPaymentBlocked => !IsPartialPaymentAllowed;
    public bool IsPartialPayment        => !IsTotalPayment;

    // ── Breakdown (datos del backend) ──────────────────────────────────────────

    public bool HasCurrentDueBreakdown
        => (_preview?.OverduePrincipalAmount ?? 0m) > 0m
        || (_preview?.CurrentMoratoryAmount  ?? 0m) > 0m;

    public string CurrentDueBreakdownText
        => $"Cobro de hoy: cuota vencida {FormatMoney(_preview?.OverduePrincipalAmount ?? 0m)}"
         + ((_preview?.CurrentMoratoryAmount ?? 0m) > 0m
            ? $" + mora actual {FormatMoney(_preview?.CurrentMoratoryAmount ?? 0m)}"
            : string.Empty);

    public Color TotalPaymentBg     => IsTotalPayment   ? _chipSelected : Colors.White;
    public Color PartialPaymentBg   => IsPartialPayment ? _chipSelected : Colors.White;
    public Color TotalPaymentText   => IsTotalPayment   ? Colors.White  : _textDark;
    public Color PartialPaymentText => IsPartialPayment ? Colors.White  : _textDark;

    // ── Método de pago ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsCashSelected), nameof(IsTransferSelected), nameof(IsCardSelected),
        nameof(CashBg), nameof(TransferBg), nameof(CardBg),
        nameof(CashStroke), nameof(TransferStroke), nameof(CardStroke),
        nameof(CashText), nameof(TransferText), nameof(CardText),
        nameof(IsProofSectionVisible))]
    private PaymentMethod _selectedMethod = PaymentMethod.Cash;

    public bool IsCashEnabled     => paymentSettings.CashEnabled;
    public bool IsTransferEnabled => paymentSettings.TransferEnabled;
    public bool IsCardEnabled     => paymentSettings.CardEnabled;

    public bool IsCashSelected     => SelectedMethod == PaymentMethod.Cash;
    public bool IsTransferSelected => SelectedMethod == PaymentMethod.Transfer;
    public bool IsCardSelected     => SelectedMethod == PaymentMethod.Card;

    public Color CashBg         => IsCashSelected     ? _chipSoft     : Colors.White;
    public Color TransferBg     => IsTransferSelected ? _chipSoft     : Colors.White;
    public Color CardBg         => IsCardSelected     ? _chipSoft     : Colors.White;
    public Color CashStroke     => IsCashSelected     ? _chipSelected : _chipUnselStroke;
    public Color TransferStroke => IsTransferSelected ? _chipSelected : _chipUnselStroke;
    public Color CardStroke     => IsCardSelected     ? _chipSelected : _chipUnselStroke;
    public Color CashText       => IsCashSelected     ? _chipSelected : _textDark;
    public Color TransferText   => IsTransferSelected ? _chipSelected : _textDark;
    public Color CardText       => IsCardSelected     ? _chipSelected : _textDark;

    // ── Notas y fecha ──────────────────────────────────────────────────────────

    [ObservableProperty] private string   _notes           = string.Empty;
    [ObservableProperty] private DateTime _paymentDateTime = DateTime.Now;

    public string PaymentDateTimeText
        => DateTime.Now.ToString("dddd d 'de' MMMM yyyy, HH:mm", new CultureInfo("es-MX")) is var raw
           ? char.ToUpper(raw[0]) + raw[1..]
           : string.Empty;

    // ── Comprobante de pago ────────────────────────────────────────────────────

    public bool IsProofSectionVisible
        => SelectedMethod is PaymentMethod.Transfer or PaymentMethod.Card;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProof), nameof(HasNoProof), nameof(ProofImageSource))]
    private string? _proofImagePath;

    public bool HasProof   => ProofImagePath is not null;
    public bool HasNoProof => ProofImagePath is null;
    public ImageSource? ProofImageSource
        => ProofImagePath is not null ? ImageSource.FromFile(ProofImagePath) : null;

    // ── Resumen de adeudos ─────────────────────────────────────────────────────

    public ObservableCollection<PaymentDebtSummaryItem> DebtSummaryItems { get; } = [];
    public bool HasDebtSummaryItems => DebtSummaryItems.Count > 0;

    // ── Carga ──────────────────────────────────────────────────────────────────

    public void Load(RegisterPaymentSnapshot snapshot)
    {
        _clientId = snapshot.ClientId;
        _debtId   = snapshot.DebtId;

        ClientNameUpper = snapshot.ClientNameUpper;
        StatusLabel     = snapshot.StatusLabel;
        StatusColor     = snapshot.StatusColor;

        _suppressAmountPreview = true;
        TotalDebt        = "—";
        CurrentDueAmount = "—";
        AmountToPay      = "0.00";
        _suppressAmountPreview = false;

        IsTotalPayment          = true;
        IsPartialPaymentAllowed = true;
        DebtSummaryItems.Clear();
        _preview = null;

        SelectedMethod = paymentSettings.CashEnabled     ? PaymentMethod.Cash
                       : paymentSettings.TransferEnabled ? PaymentMethod.Transfer
                                                         : PaymentMethod.Card;
        Notes           = string.Empty;
        PaymentDateTime = DateTime.Now;
        ProofImagePath  = null;

        OnPropertyChanged(nameof(PaymentDateTimeText));
        OnPropertyChanged(nameof(StatusBadgeBg));
        OnPropertyChanged(nameof(HasCurrentDueBreakdown));
        OnPropertyChanged(nameof(CurrentDueBreakdownText));
        OnPropertyChanged(nameof(HasDebtSummaryItems));

        _ = LoadInitialPreviewAsync();
    }

    // ── Preview backend ────────────────────────────────────────────────────────

    private async Task LoadInitialPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(_clientId)) return;

        IsLoading = true;
        try
        {
            var req     = new PreviewPaymentRequest(Amount: 0m, IsTotalPayment: true, PaymentDateTime: null);
            var preview = await paymentService.PreviewAsync(_clientId, req);
            ApplyPreview(preview, isInitial: true);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo cargar el resumen de deuda", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos cargar el resumen. Verifica tu conexión.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void TriggerPreviewDebounced()
    {
        _previewCts.Cancel();
        _previewCts = new CancellationTokenSource();
        _ = DebounceAndPreviewAsync(_previewCts.Token);
    }

    private async Task DebounceAndPreviewAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(600, ct);
            await LoadUserPreviewAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    private async Task LoadUserPreviewAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_clientId)) return;

        var amount = ParseMoney(AmountToPay);
        if (amount <= 0m) return;

        IsLoading = true;
        try
        {
            var req     = new PreviewPaymentRequest(amount, IsTotalPayment, DateTime.Now);
            var preview = await paymentService.PreviewAsync(_clientId, req, ct);
            ApplyPreview(preview, isInitial: false);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex) { await ShowAlertAsync("Error al actualizar vista previa", ex.Message); }
        catch (Exception) { }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyPreview(PaymentPreviewResponse preview, bool isInitial)
    {
        _preview = preview;

        TotalDebt        = FormatMoney(preview.TotalDebtAmount);
        CurrentDueAmount = FormatMoney(preview.CurrentDueAmount);
        IsPartialPaymentAllowed = preview.IsPartialPaymentAllowed;

        if (preview.StatusLabel is not null)
        {
            StatusLabel = preview.StatusLabel;
            StatusColor = preview.StatusLabel switch
            {
                "Atrasado"    => Color.FromArgb("#F04438"),
                "Pendiente"   => Color.FromArgb("#F79009"),
                "Al corriente"=> Color.FromArgb("#12B76A"),
                "Liquidado"   => Color.FromArgb("#667085"),
                _             => StatusColor
            };
        }

        if (isInitial)
        {
            _suppressAmountPreview = true;
            AmountToPay = preview.SuggestedAmountToPay > 0m
                ? preview.SuggestedAmountToPay.ToString("N2")
                : "0.00";
            _suppressAmountPreview = false;
        }

        DebtSummaryItems.Clear();
        foreach (var item in preview.DebtSummaryItems ?? [])
            DebtSummaryItems.Add(MapToUiItem(item));

        OnPropertyChanged(nameof(HasDebtSummaryItems));
        OnPropertyChanged(nameof(StatusBadgeBg));
        OnPropertyChanged(nameof(HasCurrentDueBreakdown));
        OnPropertyChanged(nameof(CurrentDueBreakdownText));
    }

    private static PaymentDebtSummaryItem MapToUiItem(PaymentDebtSummaryItemModel model)
    {
        var (bg, text) = model.Status switch
        {
            "Vencido"   => (Color.FromArgb("#FFF1F3"), Color.FromArgb("#F04438")),
            "Pendiente" => (Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708")),
            "Parcial"   => (Color.FromArgb("#EFF8FF"), Color.FromArgb("#1570EF")),
            _           => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#667085"))
        };
        return new PaymentDebtSummaryItem(
            model.Description,
            model.DueDate,
            FormatMoney(model.PrincipalAmount),
            FormatMoney(model.MoratoryAmount),
            FormatMoney(model.TotalAmount),
            model.Status,
            bg, text);
    }

    // ── Comandos ───────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task BackAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.Navigation.PopAsync();
    }

    [RelayCommand]
    void SelectTotalPayment()
    {
        _previewCts.Cancel();
        IsTotalPayment = true;

        _suppressAmountPreview = true;
        AmountToPay = _preview?.TotalDebtAmount is > 0m tot
            ? tot.ToString("N2")
            : "0.00";
        _suppressAmountPreview = false;

        TriggerPreviewDebounced();
    }

    [RelayCommand]
    void SelectPartialPayment()
    {
        if (!IsPartialPaymentAllowed) return;

        _previewCts.Cancel();
        IsTotalPayment = false;

        _suppressAmountPreview = true;
        AmountToPay = _preview?.ScheduledPaymentAmount is > 0m sched
            ? sched.ToString("N2")
            : string.Empty;
        _suppressAmountPreview = false;

        TriggerPreviewDebounced();
    }

    [RelayCommand] void SelectCash()     => SelectedMethod = PaymentMethod.Cash;
    [RelayCommand] void SelectTransfer() => SelectedMethod = PaymentMethod.Transfer;
    [RelayCommand] void SelectCard()     => SelectedMethod = PaymentMethod.Card;

    [RelayCommand]
    async Task AttachProofFromCameraAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is not null) ProofImagePath = photo.FullPath;
        }
        catch { }
    }

    [RelayCommand]
    async Task AttachProofFromGalleryAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is not null) ProofImagePath = photo.FullPath;
        }
        catch { }
    }

    [RelayCommand]
    void RemoveProof() => ProofImagePath = null;

    [RelayCommand(CanExecute = nameof(CanConfirmPayment))]
    async Task ConfirmPaymentAsync()
    {
        var amount = ParseMoney(AmountToPay);
        if (amount <= 0m)
        {
            await ShowAlertAsync("Monto inválido", "Ingresa un monto mayor a cero para continuar.");
            return;
        }

        var confirmed = await ShowConfirmAsync(
            "Confirmar pago",
            $"¿Registrar {FormatMoney(amount)} como pago {(IsTotalPayment ? "total" : "parcial")}?");

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var method = SelectedMethod switch
            {
                PaymentMethod.Transfer => "Transferencia",
                PaymentMethod.Card     => "Tarjeta",
                _                      => "Efectivo"
            };

            var request = new RegisterPaymentRequest(
                Amount:          amount,
                IsTotalPayment:  IsTotalPayment,
                Method:          method,
                PaymentDateTime: PaymentDateTime,
                Notes:           string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                ProofFilePath:   ProofImagePath);

            await paymentService.RegisterAsync(_clientId, request);

            await ShowAlertAsync("Pago registrado", "El pago fue registrado correctamente.");

            if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
                await p.Navigation.PopAsync();
        }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo registrar el pago", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos registrar el pago. Verifica tu conexión e intenta de nuevo.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    bool CanConfirmPayment() => !IsLoading;

    [RelayCommand]
    async Task GenerateTicketAsync()
        => await ShowAlertAsync("Próximamente", "La generación de tickets estará disponible después de confirmar el pago.");

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }

    private static async Task<bool> ShowConfirmAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            return await p.DisplayAlert(title, msg, "Confirmar", "Cancelar");
        return false;
    }

    private static decimal ParseMoney(string raw)
    {
        var clean = raw.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            || decimal.TryParse(clean, NumberStyles.Number, CultureInfo.CurrentCulture, out v)
            ? v : 0m;
    }

    private static string FormatMoney(decimal amount) => $"${amount:N2}";

    public enum PaymentMethod { Cash, Transfer, Card }
}
