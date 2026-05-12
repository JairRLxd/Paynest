using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Core.Models.Cobrador.Clients.RegisterPayment;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.Pages;
using Paynest.Infrastructure;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;
using System.Collections.ObjectModel;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class ClientDetailViewModel : ObservableObject
{
    private readonly GetClientDetailUseCase      _getClientDetail;
    private readonly DownloadTicketUseCase       _downloadTicket;
    private readonly ICollectorClientService     _clientService;
    private readonly CollectorDataRefreshService _refreshService;

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
    [ObservableProperty] private string _email             = string.Empty;
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
    [NotifyPropertyChangedFor(nameof(PersonalInfoBadgeBg), nameof(PersonalInfoBadgeTextColor), nameof(PersonalInfoBadgeLabel))]
    private bool _personalInfoCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DocumentsBadgeBg), nameof(DocumentsBadgeTextColor), nameof(DocumentsBadgeLabel))]
    private bool _documentsCompleted;

    public Color  PersonalInfoBadgeBg        => PersonalInfoCompleted ? Color.FromArgb("#ECFDF3") : Color.FromArgb("#FFF7ED");
    public Color  PersonalInfoBadgeTextColor => PersonalInfoCompleted ? Color.FromArgb("#027A48") : Color.FromArgb("#B54708");
    public string PersonalInfoBadgeLabel     => PersonalInfoCompleted ? "Completo" : "Pendiente";

    public Color  DocumentsBadgeBg        => DocumentsCompleted ? Color.FromArgb("#ECFDF3") : Color.FromArgb("#FFF7ED");
    public Color  DocumentsBadgeTextColor => DocumentsCompleted ? Color.FromArgb("#027A48") : Color.FromArgb("#B54708");
    public string DocumentsBadgeLabel     => DocumentsCompleted ? "Completo" : "Pendiente";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailLoaded))]
    private bool _isLoadingDetail;

    public bool IsDetailLoaded => !IsLoadingDetail;

    public ObservableCollection<ClientDebtItem> DebtHistory { get; } = [];

    public ClientDetailViewModel(
        GetClientDetailUseCase      getClientDetail,
        DownloadTicketUseCase       downloadTicket,
        ICollectorClientService     clientService,
        CollectorDataRefreshService refreshService)
    {
        _getClientDetail = getClientDetail;
        _downloadTicket  = downloadTicket;
        _clientService   = clientService;
        _refreshService  = refreshService;
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

        IsLoadingDetail = true;
        DebtHistory.Clear();
        _ = LoadDetailAsync(ClientId);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ClientId) || IsLoadingDetail)
            return;

        await LoadDetailAsync(ClientId, ct);
    }

    private async Task LoadDetailAsync(string clientId, CancellationToken ct = default)
    {
        IsLoadingDetail = true;
        try
        {
            var result = await _getClientDetail.ExecuteAsync(clientId, ct);
            ApplyDetail(result.Profile);
            ApplyFinancialSummary(result.Financial);
            ApplyHistory(result.History);
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
        Name                   = detail.VisibleName;
        Initials               = detail.Initials;
        Email                  = detail.Email ?? string.Empty;
        Phone                  = detail.Phone ?? string.Empty;
        Curp                   = detail.Curp ?? string.Empty;
        Rfc                    = detail.Rfc ?? string.Empty;
        Address                = detail.Address ?? string.Empty;
        PostalCode             = detail.PostalCode ?? string.Empty;
        Colonia                = detail.Colonia ?? string.Empty;
        Municipio              = detail.Municipio ?? string.Empty;
        Estado                 = detail.Estado ?? string.Empty;
        RegisteredAt           = detail.DateJoined.ToString("d MMM yyyy");
        PersonalInfoCompleted  = detail.PersonalInfoCompleted;
        DocumentsCompleted     = detail.DocumentsCompleted;
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
            Title:            ToDisplayMethod(payment.Method),
            DateText:         payment.PaidAt.ToString("d MMM yyyy"),
            Amount:           $"${payment.Amount:N2}",
            Status:           status,
            IconGlyph:        glyph,
            IconBackground:   iconBg,
            IconColor:        iconColor,
            StatusBackground: statusBg,
            StatusTextColor:  statusText,
            PaymentId:        payment.PaymentId,
            ClientId:         payment.ClientId);
    }

    private static string ToDisplayMethod(string method)
        => method.Trim().ToLowerInvariant() switch
        {
            "wallet" or "paynestwallet" or "paynest_wallet" => "Saldo Paynest",
            "cash" => "Efectivo",
            "transfer" => "Transferencia",
            "card" => "Tarjeta",
            _ => string.IsNullOrWhiteSpace(method) ? "Pago" : method
        };

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
    async Task CallClientAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            await ShowAlertAsync("Sin teléfono", "Este cliente no tiene un número registrado.");
            return;
        }

        try
        {
            PhoneDialer.Default.Open(Phone);
        }
        catch (FeatureNotSupportedException)
        {
            await ShowAlertAsync("No disponible", "Tu dispositivo no soporta llamadas.");
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error", "No pudimos abrir el marcador. Verifica el número del cliente.");
        }
    }

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
    async Task DeleteClientAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Eliminar cliente",
            $"¿Estás seguro de que deseas eliminar a {Name}? Esta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar");

        if (!confirmed) return;

        try
        {
            await _clientService.DeleteClientAsync(ClientId);
            _refreshService.NotifyChanged(CollectorRefreshScope.Clients | CollectorRefreshScope.Collections);

            if (App.CurrentNavigation is { } navigation)
                await navigation.PopAsync();
        }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo eliminar", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error", "No pudimos eliminar el cliente. Intenta de nuevo.");
        }
    }

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
    async Task DownloadTicketAsync(ClientDebtItem debt)
    {
        if (string.IsNullOrWhiteSpace(debt.PaymentId) || string.IsNullOrWhiteSpace(debt.ClientId))
        {
            await ShowAlertAsync("No disponible", "El ticket no está disponible para este pago.");
            return;
        }

        var ticketUrl = $"{ApiConstants.BaseUrl.TrimEnd('/')}/api/v1/collector/clients/{debt.ClientId}/payments/{debt.PaymentId}/ticket/download";

        try
        {
            var pdfBytes = await _downloadTicket.ExecuteAsync(ticketUrl);
            var path = Path.Combine(FileSystem.CacheDirectory, $"ticket_{debt.PaymentId}.pdf");
            await File.WriteAllBytesAsync(path, pdfBytes);
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Ticket de pago - Paynest",
                File  = new ShareFile(path, "application/pdf")
            });
        }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo descargar el ticket", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error", "No pudimos descargar el ticket. Intenta de nuevo.");
        }
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
