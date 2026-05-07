using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients.CreateDebt;
using Paynest.Features.Cobrador.Clients.Models;
using Paynest.Features.Cobrador.Clients.CreateDebt.Views;
using Paynest.Infrastructure.Exceptions;
using System.Globalization;

namespace Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;

public partial class CreateDebtViewModel(ICollectorDebtService debtService) : ObservableObject
{
    private static readonly Color SelectedChip = Color.FromArgb("#2A6349");
    private static readonly Color UnselectedChip = Color.FromArgb("#EFEFEF");
    private static readonly Color SelectedText = Colors.White;
    private static readonly Color UnselectedText = Color.FromArgb("#2C2C2C");

    private CancellationTokenSource? _previewCts;
    private bool _isApplyingPreview;

    public Func<Task>? OnDebtCreated { get; set; }

    [ObservableProperty] private string _clientId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientNameUpper), nameof(HasClient), nameof(HasNoClient))]
    private string _clientName = string.Empty;

    public string ClientNameUpper => ClientName.ToUpperInvariant();
    public bool HasClient => !string.IsNullOrEmpty(ClientName);
    public bool HasNoClient => string.IsNullOrEmpty(ClientName);

    [ObservableProperty] private string _clientPhone = string.Empty;
    [ObservableProperty] private string _clientInitials = string.Empty;
    [ObservableProperty] private Color _clientAvatarColor = Color.FromArgb("#4D8A6A");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveDebtsLabel))]
    private int _activeDebts;

    public string ActiveDebtsLabel => ActiveDebts switch
    {
        0 => "Sin deudas activas",
        1 => "1 deuda activa",
        _ => $"{ActiveDebts} deudas activas"
    };

    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _amount = string.Empty;
    [ObservableProperty] private string _paymentAmount = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsUnica), nameof(IsSemanal), nameof(IsQuincenal), nameof(IsMensual),
        nameof(UnicaBg), nameof(SemanalBg), nameof(QuincenalBg), nameof(MensualBg),
        nameof(UnicaText), nameof(SemanalText), nameof(QuincenalText), nameof(MensualText),
        nameof(ShowsFirstPaymentEditor), nameof(ShowsPaymentAmount), nameof(ShowsDueDateEditor))]
    private Periodicidad _periodicidad = Periodicidad.Quincenal;

    public bool IsUnica => Periodicidad == Periodicidad.Unica;
    public bool IsSemanal => Periodicidad == Periodicidad.Semanal;
    public bool IsQuincenal => Periodicidad == Periodicidad.Quincenal;
    public bool IsMensual => Periodicidad == Periodicidad.Mensual;

    public Color UnicaBg => IsUnica ? SelectedChip : UnselectedChip;
    public Color SemanalBg => IsSemanal ? SelectedChip : UnselectedChip;
    public Color QuincenalBg => IsQuincenal ? SelectedChip : UnselectedChip;
    public Color MensualBg => IsMensual ? SelectedChip : UnselectedChip;
    public Color UnicaText => IsUnica ? SelectedText : UnselectedText;
    public Color SemanalText => IsSemanal ? SelectedText : UnselectedText;
    public Color QuincenalText => IsQuincenal ? SelectedText : UnselectedText;
    public Color MensualText => IsMensual ? SelectedText : UnselectedText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsByInstallmentAmount), nameof(IsByDueDate),
        nameof(ByInstallmentBg), nameof(ByDueDateBg),
        nameof(ByInstallmentText), nameof(ByDueDateText),
        nameof(ShowsPaymentAmount), nameof(ShowsDueDateEditor), nameof(ShowsFirstPaymentEditor))]
    private DebtCalculationMode _calculationMode = DebtCalculationMode.ByInstallmentAmount;

    public bool IsByInstallmentAmount => CalculationMode == DebtCalculationMode.ByInstallmentAmount;
    public bool IsByDueDate => CalculationMode == DebtCalculationMode.ByDueDate;
    public bool ShowsFirstPaymentEditor => !IsUnica;
    public bool ShowsPaymentAmount => IsByInstallmentAmount && !IsUnica;
    public bool ShowsDueDateEditor => IsByDueDate || IsUnica;
    public Color ByInstallmentBg => IsByInstallmentAmount ? SelectedChip : UnselectedChip;
    public Color ByDueDateBg => IsByDueDate ? SelectedChip : UnselectedChip;
    public Color ByInstallmentText => IsByInstallmentAmount ? SelectedText : UnselectedText;
    public Color ByDueDateText => IsByDueDate ? SelectedText : UnselectedText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartDateText))]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FirstPaymentDateText))]
    private DateTime _firstPaymentDate = DateTime.Today.AddDays(14);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DueDateText))]
    private DateTime _dueDate = DateTime.Today.AddDays(28);

    public string StartDateText => FormatDate(StartDate);
    public string FirstPaymentDateText => FormatDate(FirstPaymentDate);
    public string DueDateText => FormatDate(DueDate);

    [ObservableProperty] private string _interestRate = string.Empty;
    [ObservableProperty] private string _moratoryRate = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary))] private string _summaryPrincipal = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary), nameof(HasInterestSummary))] private string _summaryInterestLabel = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary), nameof(HasInterestSummary))] private string _summaryInterestAmount = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary))] private string _summaryTotalToCollect = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary), nameof(HasInstallmentSummary))] private string _summaryInstallmentTitle = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary), nameof(HasInstallmentSummary))] private string _summaryInstallmentAmount = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientSummary))] private string _summaryFootnote = string.Empty;
    [ObservableProperty] private string _scheduledPaymentAmount = string.Empty;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotLoading))] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasClientError))] private string? _clientError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasDescriptionError))] private string? _descriptionError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasAmountError))] private string? _amountError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPaymentAmountError))] private string? _paymentAmountError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasDueDateError))] private string? _dueDateError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasGeneralError))] private string? _generalError;

    public bool HasClientError => ClientError is not null;
    public bool HasDescriptionError => DescriptionError is not null;
    public bool HasAmountError => AmountError is not null;
    public bool HasPaymentAmountError => PaymentAmountError is not null;
    public bool HasDueDateError => DueDateError is not null;
    public bool HasGeneralError => GeneralError is not null;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPreview))] private string _previewPrimary = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPreview))] private string _previewSecondary = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasPreview))] private string _previewCaption = string.Empty;
    public bool HasPreview => !string.IsNullOrWhiteSpace(PreviewPrimary);
    public bool HasClientSummary => !string.IsNullOrWhiteSpace(SummaryTotalToCollect);
    public bool HasInterestSummary => !string.IsNullOrWhiteSpace(SummaryInterestAmount);
    public bool HasInstallmentSummary => !string.IsNullOrWhiteSpace(SummaryInstallmentAmount);

    partial void OnClientIdChanged(string value)
    {
        SchedulePreviewRefresh();
        _ = RefreshClientOpenSummaryAsync(value);
    }
    partial void OnStartDateChanged(DateTime value)
    {
        if (_isApplyingPreview)
            return;

        EnsureDatesForPeriodicity();
        SchedulePreviewRefresh();
    }

    partial void OnFirstPaymentDateChanged(DateTime value)
    {
        if (_isApplyingPreview)
            return;

        if (value.Date < StartDate.Date)
            FirstPaymentDate = StartDate.Date;

        if (DueDate.Date < FirstPaymentDate.Date)
            DueDate = FirstPaymentDate.Date;

        SchedulePreviewRefresh();
    }

    partial void OnDueDateChanged(DateTime value)
    {
        if (_isApplyingPreview)
            return;

        if (value.Date < FirstPaymentDate.Date)
            DueDate = FirstPaymentDate.Date;

        SchedulePreviewRefresh();
    }

    partial void OnPeriodicidadChanged(Periodicidad value)
    {
        if (_isApplyingPreview)
            return;

        EnsureDatesForPeriodicity();
        SchedulePreviewRefresh();
    }

    partial void OnCalculationModeChanged(DebtCalculationMode value)
    {
        if (_isApplyingPreview)
            return;

        EnsureDatesForPeriodicity();
        SchedulePreviewRefresh();
    }

    partial void OnAmountChanged(string value) => SchedulePreviewRefresh();
    partial void OnPaymentAmountChanged(string value) => SchedulePreviewRefresh();
    partial void OnInterestRateChanged(string value) => SchedulePreviewRefresh();
    partial void OnMoratoryRateChanged(string value) => SchedulePreviewRefresh();

    [RelayCommand]
    async Task SelectClientAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return;

        await page.DisplayAlertAsync(
            "Seleccionar cliente",
            "La selección real de clientes aún no está integrada en este flujo. Por ahora crea la deuda desde el detalle del cliente.",
            "Entendido");
    }

    [RelayCommand] void SelectUnica() => Periodicidad = Periodicidad.Unica;
    [RelayCommand] void SelectSemanal() => Periodicidad = Periodicidad.Semanal;
    [RelayCommand] void SelectQuincenal() => Periodicidad = Periodicidad.Quincenal;
    [RelayCommand] void SelectMensual() => Periodicidad = Periodicidad.Mensual;
    [RelayCommand] void SelectByInstallmentAmount() => CalculationMode = DebtCalculationMode.ByInstallmentAmount;
    [RelayCommand] void SelectByDueDate() => CalculationMode = DebtCalculationMode.ByDueDate;

    [RelayCommand]
    async Task PickStartDateAsync()
    {
        await OpenCalendarAsync("Fecha de inicio", StartDate, new DateTime(2020, 1, 1), selected =>
        {
            StartDate = selected.Date;
            EnsureDatesForPeriodicity();
        });
    }

    [RelayCommand]
    async Task PickFirstPaymentDateAsync()
    {
        await OpenCalendarAsync("Primer pago", FirstPaymentDate, StartDate, selected =>
        {
            FirstPaymentDate = selected.Date;
            EnsureDatesForPeriodicity();
        });
    }

    [RelayCommand]
    async Task PickDueDateAsync()
    {
        await OpenCalendarAsync("Fecha final", DueDate, FirstPaymentDate, selected => DueDate = selected.Date);
    }

    [RelayCommand]
    async Task CreateAsync()
    {
        if (!Validate())
            return;

        var request = BuildCreateRequest();
        if (request is null)
            return;

        IsLoading = true;
        GeneralError = null;

        try
        {
            await debtService.CreateAsync(ClientId, request);
            if (OnDebtCreated is not null)
                await OnDebtCreated();

            if (App.CurrentNavigation is { } navigation)
                await navigation.PopAsync();
        }
        catch (ApiException ex)
        {
            GeneralError = ex.Message;
        }
        catch (Exception)
        {
            GeneralError = "No pudimos crear la deuda. Intenta nuevamente.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task BackAsync()
    {
        if (App.CurrentNavigation is { } navigation)
            await navigation.PopAsync();
    }

    public void LoadClientContext(ClientSummary client)
    {
        ClientId = client.Id;
        ClientName = client.Name;
        ClientInitials = client.Initials;
        ClientAvatarColor = client.AvatarColor;
        ClientPhone = string.Empty;
        ClientError = null;
    }

    public void ResetForDirectCreation()
    {
        ClientId = string.Empty;
        ClientName = string.Empty;
        ClientPhone = string.Empty;
        ClientInitials = string.Empty;
        ClientAvatarColor = Color.FromArgb("#4D8A6A");
        ActiveDebts = 0;
        ClientError = null;
        GeneralError = "Por ahora selecciona el cliente desde su detalle para crear una deuda sin usar datos mock.";
        ClearPreviewState();
    }

    private async Task OpenCalendarAsync(string title, DateTime initialDate, DateTime minimumDate, Action<DateTime> onSelect)
    {
        var page = new CalendarPickerPage();
        if (page.BindingContext is CalendarPickerViewModel vm)
            vm.Load(title, initialDate, minimumDate, onSelect);

        if (App.CurrentNavigation is { } navigation)
            await navigation.PushModalAsync(page);
    }

    private void EnsureDatesForPeriodicity()
    {
        if (Periodicidad == Periodicidad.Unica)
        {
            if (FirstPaymentDate.Date != StartDate.Date)
                FirstPaymentDate = StartDate;
            if (DueDate.Date != StartDate.Date)
                DueDate = StartDate;
            return;
        }

        if (FirstPaymentDate.Date < StartDate.Date)
            FirstPaymentDate = StartDate.AddDays(1);

        if (DueDate.Date < FirstPaymentDate.Date)
            DueDate = FirstPaymentDate.Date;
    }

    private void SchedulePreviewRefresh()
    {
        if (_isApplyingPreview)
            return;

        _previewCts?.Cancel();

        var request = BuildPreviewRequest();
        if (request is null)
        {
            ClearPreviewState();
            return;
        }

        var cts = new CancellationTokenSource();
        _previewCts = cts;
        _ = RefreshPreviewAsync(request, cts.Token);
    }

    private async Task RefreshPreviewAsync(CollectorDebtPreviewRequest request, CancellationToken ct)
    {
        try
        {
            await Task.Delay(250, ct);
            var response = await debtService.PreviewAsync(ClientId, request, ct);
            if (ct.IsCancellationRequested)
                return;

            ApplyPreview(response);
            GeneralError = null;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException ex)
        {
            if (!ct.IsCancellationRequested)
            {
                ClearPreviewState();
                GeneralError = ex.Message;
            }
        }
        catch (Exception)
        {
            if (!ct.IsCancellationRequested)
            {
                ClearPreviewState();
                GeneralError = "No pudimos calcular la vista previa de la deuda.";
            }
        }
    }

    private async Task RefreshClientOpenSummaryAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            ActiveDebts = 0;
            return;
        }

        try
        {
            var summary = await debtService.GetOpenSummaryAsync(clientId);
            ActiveDebts = summary.OpenDebtsCount;
        }
        catch
        {
            ActiveDebts = 0;
        }
    }

    private void ApplyPreview(CollectorDebtPreviewResponse response)
    {
        _isApplyingPreview = true;
        try
        {
            StartDate = response.StartDate.Date;
            FirstPaymentDate = response.FirstPaymentDate.Date;
            DueDate = response.DueDate.Date;
        }
        finally
        {
            _isApplyingPreview = false;
        }

        SummaryPrincipal = FormatMoney(response.PrincipalAmount);
        SummaryInterestLabel = response.InterestAmount > 0 && ParsePercent(InterestRate) is { } rate && rate > 0
            ? $"Interés único ({rate:0.##}%)"
            : string.Empty;
        SummaryInterestAmount = response.InterestAmount > 0 ? FormatMoney(response.InterestAmount) : string.Empty;
        SummaryTotalToCollect = FormatMoney(response.TotalAmount);
        ScheduledPaymentAmount = FormatMoney(response.ScheduledPaymentAmount);

        if (response.InstallmentsCount <= 1 || Periodicidad == Periodicidad.Unica)
        {
            SummaryInstallmentTitle = "Pago único";
            SummaryInstallmentAmount = FormatMoney(response.TotalAmount);
            SummaryFootnote = $"Se liquida el {FormatDate(FirstPaymentDate)}.";
            PreviewPrimary = $"1 pago de {FormatMoney(response.TotalAmount)}";
            PreviewSecondary = $"Se liquida el {FormatDate(FirstPaymentDate)}";
            PreviewCaption = "Vista previa oficial calculada por el backend.";
            return;
        }

        SummaryInstallmentTitle = IsByDueDate
            ? $"Pago {PeriodicidadToText(Periodicidad).ToLowerInvariant()} sugerido"
            : $"Pago {PeriodicidadToText(Periodicidad).ToLowerInvariant()}";
        SummaryInstallmentAmount = FormatMoney(response.ScheduledPaymentAmount);
        SummaryFootnote = $"Serán {response.InstallmentsCount} pagos; el último se ajusta a {FormatMoney(response.LastPaymentAmount)} y termina el {FormatDate(DueDate)}.";
        PreviewPrimary = $"{response.InstallmentsCount} pagos · termina el {FormatDate(DueDate)}";
        PreviewSecondary = $"Primer pago {FormatDate(FirstPaymentDate)} · regular {FormatMoney(response.ScheduledPaymentAmount)} · último {FormatMoney(response.LastPaymentAmount)}";
        PreviewCaption = "Vista previa oficial calculada por el backend.";
    }

    private CollectorDebtPreviewRequest? BuildPreviewRequest()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            return null;

        if (string.IsNullOrWhiteSpace(Description))
            return null;

        var totalAmount = ParseMoney(Amount);
        if (totalAmount is null || totalAmount <= 0)
            return null;

        if (!ValidatePreviewDates())
            return null;

        decimal? paymentAmount = null;
        if (IsByInstallmentAmount && !IsUnica)
        {
            paymentAmount = ParseMoney(PaymentAmount);
            if (paymentAmount is null || paymentAmount <= 0)
                return null;
        }

        return new CollectorDebtPreviewRequest(
            Description.Trim(),
            totalAmount.Value,
            Periodicidad,
            CalculationMode,
            DateOnly.FromDateTime(StartDate),
            DateOnly.FromDateTime(IsUnica ? StartDate : FirstPaymentDate),
            DateOnly.FromDateTime(IsUnica ? StartDate : DueDate),
            paymentAmount,
            ParsePercent(InterestRate),
            ParsePercent(MoratoryRate),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());
    }

    private CollectorDebtCreateRequest? BuildCreateRequest()
    {
        var totalAmount = ParseMoney(Amount);
        if (totalAmount is null || totalAmount <= 0)
            return null;

        decimal? paymentAmount = null;
        if (IsByInstallmentAmount && !IsUnica)
        {
            paymentAmount = ParseMoney(PaymentAmount);
            if (paymentAmount is null || paymentAmount <= 0)
                return null;
        }

        return new CollectorDebtCreateRequest(
            Description.Trim(),
            totalAmount.Value,
            Periodicidad,
            CalculationMode,
            DateOnly.FromDateTime(StartDate),
            DateOnly.FromDateTime(IsUnica ? StartDate : FirstPaymentDate),
            DateOnly.FromDateTime(IsUnica ? StartDate : DueDate),
            paymentAmount,
            ParsePercent(InterestRate),
            ParsePercent(MoratoryRate),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());
    }

    private bool Validate()
    {
        ClientError = string.IsNullOrWhiteSpace(ClientId) ? "Selecciona un cliente para continuar" : null;
        DescriptionError = string.IsNullOrWhiteSpace(Description) ? "Ingresa una descripción para la deuda" : null;

        var totalAmount = ParseMoney(Amount);
        AmountError = totalAmount is null || totalAmount <= 0 ? "El monto debe ser mayor a cero" : null;

        PaymentAmountError = null;
        DueDateError = null;

        if (!ValidatePreviewDates())
            DueDateError = "La fecha final debe ser igual o posterior al primer pago";

        if (Periodicidad != Periodicidad.Unica && IsByInstallmentAmount)
        {
            var installmentAmount = ParseMoney(PaymentAmount);
            if (installmentAmount is null || installmentAmount <= 0)
                PaymentAmountError = "Ingresa cuánto pagará por periodo";
        }

        return ClientError is null
            && DescriptionError is null
            && AmountError is null
            && PaymentAmountError is null
            && DueDateError is null;
    }

    private bool ValidatePreviewDates()
    {
        if (Periodicidad == Periodicidad.Unica)
            return true;

        return FirstPaymentDate.Date >= StartDate.Date && DueDate.Date >= FirstPaymentDate.Date;
    }

    private void ClearPreviewState()
    {
        PreviewPrimary = string.Empty;
        PreviewSecondary = string.Empty;
        PreviewCaption = string.Empty;
        SummaryPrincipal = string.Empty;
        SummaryInterestLabel = string.Empty;
        SummaryInterestAmount = string.Empty;
        SummaryTotalToCollect = string.Empty;
        SummaryInstallmentTitle = string.Empty;
        SummaryInstallmentAmount = string.Empty;
        SummaryFootnote = string.Empty;
        ScheduledPaymentAmount = string.Empty;
    }

    private static decimal? ParseMoney(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var raw = rawValue.Replace(",", "").Replace("$", "").Trim();
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            ? value
            : null;
    }

    private static decimal? ParsePercent(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var raw = rawValue.Replace("%", "").Replace(",", "").Trim();
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            ? value
            : null;
    }

    private static string FormatMoney(decimal amount) => $"${amount:0.##}";

    private static string FormatDate(DateTime date)
    {
        var text = date.ToString("dd MMM yyyy", new CultureInfo("es-MX"));
        return char.ToUpper(text[0]) + text[1..];
    }

    private static string PeriodicidadToText(Periodicidad periodicidad)
        => periodicidad switch
        {
            Periodicidad.Semanal => "Semanal",
            Periodicidad.Quincenal => "Quincenal",
            Periodicidad.Mensual => "Mensual",
            _ => "Única"
        };
}
