using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Features.Cobrador.Clients.ComprobanteViewer.ViewModels;
using Paynest.Features.Cobrador.Clients.ComprobanteViewer.Views;
using Paynest.Features.Cobrador.Clients.Detail.Views;
using Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;
using Paynest.Features.Cobrador.Clients.CreateDebt.Views;
using Paynest.Features.Cobrador.Clients.Edit.ViewModels;
using Paynest.Features.Cobrador.Clients.Edit.Views;
using Paynest.Features.Cobrador.Clients.Models;
using Paynest.Features.Cobrador.Clients.RegisterPayment.ViewModels;
using Paynest.Features.Cobrador.Clients.RegisterPayment.Views;
using Paynest.Infrastructure.Exceptions;
using System.Collections.ObjectModel;

namespace Paynest.Features.Cobrador.Clients.Detail.ViewModels;

public partial class ClientDetailViewModel : ObservableObject
{
    private readonly ICollectorClientService  _clientService;
    private readonly ICollectorPaymentService _paymentService;

    [ObservableProperty] private string _clientId    = string.Empty;
    [ObservableProperty] private string _name        = string.Empty;
    [ObservableProperty] private string _initials    = string.Empty;
    [ObservableProperty] private Color  _avatarColor = Color.FromArgb("#4D8A6A");
    [ObservableProperty] private string _phone       = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto), nameof(HasNoPhoto), nameof(PhotoSource))]
    private string? _photoUrl;

    public bool HasPhoto   => PhotoUrl is not null;
    public bool HasNoPhoto => PhotoUrl is null;
    public ImageSource? PhotoSource
        => PhotoUrl is not null
            ? (PhotoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? ImageSource.FromUri(new Uri(PhotoUrl))
                : ImageSource.FromFile(PhotoUrl))
            : null;

    [ObservableProperty] private string _totalDebt         = "—";
    [ObservableProperty] private string _totalPaid         = "—";
    [ObservableProperty] private string _dueLabel          = "—";
    [ObservableProperty] private string _statusLabel       = "—";
    [ObservableProperty] private string _statusDescription = string.Empty;
    [ObservableProperty] private Color  _statusColor       = Color.FromArgb("#667085");
    [ObservableProperty] private string _curp              = string.Empty;
    [ObservableProperty] private string _rfc               = string.Empty;
    [ObservableProperty] private string _address           = string.Empty;
    [ObservableProperty] private string _postalCode        = string.Empty;
    [ObservableProperty] private string _colonia           = string.Empty;
    [ObservableProperty] private string _municipio         = string.Empty;
    [ObservableProperty] private string _estado            = string.Empty;
    [ObservableProperty] private string _registeredAt      = string.Empty;
    [ObservableProperty] private string _notes             = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailLoaded))]
    private bool _isLoadingDetail;

    public bool IsDetailLoaded => !IsLoadingDetail;

    public ObservableCollection<ClientDebtItem> DebtHistory { get; } = [];

    public ClientDetailViewModel(
        ICollectorClientService  clientService,
        ICollectorPaymentService paymentService)
    {
        _clientService  = clientService;
        _paymentService = paymentService;
    }

    public void Load(ClientSummary client)
    {
        ClientId    = client.Id;
        Name        = client.Name;
        Initials    = client.Initials;
        AvatarColor = client.AvatarColor;
        StatusLabel = client.Status;
        TotalDebt   = client.Amount;
        DueLabel    = client.DateText;
        StatusColor = client.Status switch
        {
            "Atrasado"     => Color.FromArgb("#F04438"),
            "Pendiente"    => Color.FromArgb("#F79009"),
            "Al corriente" => Color.FromArgb("#12B76A"),
            "Liquidado"    => Color.FromArgb("#667085"),
            _              => Color.FromArgb("#667085")
        };

        DebtHistory.Clear();
        _ = LoadDetailAsync(client.Id);
    }

    private async Task LoadDetailAsync(string clientId, CancellationToken ct = default)
    {
        IsLoadingDetail = true;
        try
        {
            var detailTask  = _clientService.GetClientDetailAsync(clientId, ct);
            var summaryTask = _clientService.GetFinancialSummaryAsync(clientId, ct);
            var historyTask = _paymentService.GetHistoryAsync(clientId, ct);

            await Task.WhenAll(detailTask, summaryTask, historyTask);

            ApplyDetail(detailTask.Result);
            ApplyFinancialSummary(summaryTask.Result);
            ApplyHistory(historyTask.Result);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo cargar el detalle", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos cargar el detalle del cliente.");
        }
        finally
        {
            IsLoadingDetail = false;
        }
    }

    private void ApplyDetail(CollectorClientDetailResponse detail)
    {
        Name         = detail.VisibleName;
        Initials     = detail.Initials;
        Phone        = detail.Phone ?? string.Empty;
        Curp         = detail.Curp ?? string.Empty;
        Rfc          = detail.Rfc ?? string.Empty;
        Address      = detail.Address ?? string.Empty;
        PostalCode   = detail.PostalCode ?? string.Empty;
        Colonia      = detail.Colonia ?? string.Empty;
        Municipio    = detail.Municipio ?? string.Empty;
        Estado       = detail.Estado ?? string.Empty;
        RegisteredAt = detail.DateJoined.ToString("d MMM yyyy");
    }

    private void ApplyFinancialSummary(CollectorClientFinancialSummaryResponse financial)
    {
        TotalDebt         = $"${financial.TotalDebtAmount:N2}";
        TotalPaid         = $"${financial.TotalPaidAmount:N2}";
        StatusLabel       = financial.StatusLabel;
        StatusDescription = financial.StatusDescription;
        DueLabel          = financial.NextDueDate?.ToString("d MMM yyyy") ?? "—";
        StatusColor       = financial.StatusLabel switch
        {
            "Atrasado"     => Color.FromArgb("#F04438"),
            "Pendiente"    => Color.FromArgb("#F79009"),
            "Al corriente" => Color.FromArgb("#12B76A"),
            "Liquidado"    => Color.FromArgb("#667085"),
            _              => Color.FromArgb("#667085")
        };
    }

    private void ApplyHistory(IReadOnlyList<PaymentHistoryItemModel> history)
    {
        DebtHistory.Clear();
        foreach (var item in history)
            DebtHistory.Add(MapToPaymentItem(item));
    }

    private static ClientDebtItem MapToPaymentItem(PaymentHistoryItemModel payment)
    {
        var status = payment.IsTotalPayment ? "Pagado" : "Parcial";
        var (glyph, iconBg, iconColor, statusBg, statusText) = status switch
        {
            "Pagado" => ("✓", Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48"),
                               Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48")),
            _        => ("◑", Color.FromArgb("#EFF8FF"), Color.FromArgb("#1570EF"),
                               Color.FromArgb("#EFF8FF"), Color.FromArgb("#1570EF"))
        };

        return new ClientDebtItem(
            Title:            payment.Method,
            DateText:         payment.PaidAt.ToString("d MMM yyyy"),
            Amount:           $"${payment.Amount:N2}",
            Status:           status,
            IconGlyph:        glyph,
            IconBackground:   iconBg,
            IconColor:        iconColor,
            StatusBackground: statusBg,
            StatusTextColor:  statusText);
    }

    // ── Comandos ───────────────────────────────────────────────────────────────

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
    async Task RegisterPaymentAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
        if (page.BindingContext is RegisterPaymentViewModel vm)
            vm.Load(ToPaymentSnapshot());

        if (App.CurrentNavigation is { } navigation)
            await navigation.PushAsync(page);
    }

    [RelayCommand]
    Task CallClientAsync() => ShowPendingAlertAsync("Llamar al cliente");

    [RelayCommand]
    async Task EditClientAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<EditClientPage>();
        if (page.BindingContext is EditClientViewModel vm)
            vm.Load(ToSnapshot());

        if (App.CurrentNavigation is { } navigation)
            await navigation.PushAsync(page);
    }

    [RelayCommand]
    async Task CreateDebtAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<CreateDebtPage>();
        if (page.BindingContext is CreateDebtViewModel vm)
        {
            vm.LoadClientContext(new ClientSummary(
                ClientId, Name, Initials, AvatarColor,
                RegisteredAt, TotalDebt, StatusLabel,
                Color.FromArgb("#F3F4F6"), StatusColor));
            vm.OnDebtCreated = () => LoadDetailAsync(ClientId);
        }

        if (App.CurrentNavigation is { } navigation)
            await navigation.PushAsync(page);
    }

    [RelayCommand]
    Task DeleteClientAsync() => ShowPendingAlertAsync("Eliminar cliente");

    [RelayCommand]
    async Task ViewPhotoAsync()
    {
        if (!HasPhoto || PhotoSource is null) return;

        var viewer = new PhotoViewerPage(PhotoSource);
        if (App.CurrentNavigation is { } navigation)
            await navigation.PushModalAsync(viewer, animated: true);
    }

    [RelayCommand]
    async Task ShowMenuAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? action = await page.DisplayActionSheetAsync(
            null, "Cancelar", "Eliminar cliente", "Editar cliente");

        switch (action)
        {
            case "Editar cliente":   await EditClientAsync();   break;
            case "Eliminar cliente": await DeleteClientAsync(); break;
        }
    }

    [RelayCommand]
    async Task ViewProofAsync(ClientDebtItem debt)
    {
        if (!debt.NeedsReview) return;

        var page = MauiProgram.Services.GetRequiredService<ComprobanteViewerPage>();
        if (page.BindingContext is ComprobanteViewerViewModel vm)
        {
            vm.Load(debt, Name);
            vm.OnApproved = () => UpdateDebtItem(debt, "Pagado");
            vm.OnRejected = () => UpdateDebtItem(debt, "Pendiente", needsReview: false);
        }

        if (App.CurrentNavigation is { } navigation)
            await navigation.PushAsync(page);
    }

    private void UpdateDebtItem(ClientDebtItem old, string newStatus, bool needsReview = false)
    {
        var idx = DebtHistory.IndexOf(old);
        if (idx < 0) return;

        var (glyph, iconBg, iconColor, statusBg, statusText) = newStatus switch
        {
            "Pagado" => ("✓",
                Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48"),
                Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48")),
            _ => ("•",
                Color.FromArgb("#FFF7ED"), Color.FromArgb("#F79009"),
                Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708"))
        };

        DebtHistory[idx] = old with
        {
            Status           = newStatus,
            IconGlyph        = glyph,
            IconBackground   = iconBg,
            IconColor        = iconColor,
            StatusBackground = statusBg,
            StatusTextColor  = statusText,
            NeedsReview      = needsReview
        };
    }

    private ClientProfileSnapshot ToSnapshot()
        => new(ClientId, Name, Initials, AvatarColor,
               Phone, Curp, Rfc, Address, PostalCode, Colonia, Municipio, Estado, Notes);

    private RegisterPaymentSnapshot ToPaymentSnapshot()
        => new(ClientId, Name, Name.ToUpperInvariant(), StatusLabel, StatusColor);

    private static async Task ShowPendingAlertAsync(string action)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        await page.DisplayAlertAsync(action, "Esta acción se conectará en el siguiente paso.", "Entendido");
    }

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }
}
