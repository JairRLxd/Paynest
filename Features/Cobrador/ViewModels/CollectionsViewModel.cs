using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Collections;
using Paynest.Core.Models.Cobrador.Payments;
using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.Pages;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Infrastructure.Exceptions;
using System.Collections.ObjectModel;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class CollectionsViewModel : ObservableObject
{
    private readonly GetCollectionsDashboardUseCase _getDashboard;
    private readonly GetCollectionsUseCase _getCollections;
    private readonly GetRecentCollectorPaymentsUseCase _getRecentPayments;
    private readonly ICollectorDebtService _debtService;

    private CancellationTokenSource? _reloadCts;
    private CancellationTokenSource? _sectionsCts;

    [ObservableProperty] private string _totalAdeudado = "—";
    [ObservableProperty] private string _totalDeudas = "—";
    [ObservableProperty] private string _vencidasCount = "—";
    [ObservableProperty] private string _vencidasAmount = "—";
    [ObservableProperty] private string _visibleCountSummary = string.Empty;
    [ObservableProperty] private string _cobrosHoyCount = "0 cobros";
    [ObservableProperty] private string _cobrosHoyAmount = "$0.00";
    [ObservableProperty] private string _cobrosSemanaCount = "0 cobros";
    [ObservableProperty] private string _cobrosSemanaAmount = "$0.00";
    [ObservableProperty] private string _cobradoHoyCount = "0 pagos";
    [ObservableProperty] private string _cobradoHoyAmount = "$0.00";
    [ObservableProperty] private string _cobradoSemanaCount = "0 pagos";
    [ObservableProperty] private string _cobradoSemanaAmount = "$0.00";
    [ObservableProperty] private string _recentActivitySummary = "Aún no hay pagos recientes.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSections), nameof(IsEmpty))]
    private bool _isLoading;

    public bool HasSections => !IsLoading && Sections.Count > 0;
    public bool IsEmpty => !IsLoading && Sections.Count == 0;
    public bool HasRecentPayments => RecentPayments.Count > 0;
    public bool IsRecentPaymentsEmpty => !HasRecentPayments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(AllFilterBg), nameof(OverdueFilterBg), nameof(TodayFilterBg), nameof(WeekFilterBg),
        nameof(UpcomingFilterBg), nameof(NewFilterBg),
        nameof(AllFilterText), nameof(OverdueFilterText), nameof(TodayFilterText), nameof(WeekFilterText),
        nameof(UpcomingFilterText), nameof(NewFilterText))]
    private CollectionListFilter _selectedFilter = CollectionListFilter.All;

    public ObservableCollection<CollectionDebtSection> Sections { get; } = [];
    public ObservableCollection<CollectionRecentPaymentItem> RecentPayments { get; } = [];

    public Color AllFilterBg      => SelectedFilter == CollectionListFilter.All      ? Color.FromArgb("#23935D") : Colors.White;
    public Color OverdueFilterBg  => SelectedFilter == CollectionListFilter.Overdue  ? Color.FromArgb("#23935D") : Colors.White;
    public Color TodayFilterBg    => SelectedFilter == CollectionListFilter.Today    ? Color.FromArgb("#23935D") : Colors.White;
    public Color WeekFilterBg     => SelectedFilter == CollectionListFilter.ThisWeek ? Color.FromArgb("#23935D") : Colors.White;
    public Color UpcomingFilterBg => SelectedFilter == CollectionListFilter.Upcoming ? Color.FromArgb("#23935D") : Colors.White;
    public Color NewFilterBg      => SelectedFilter == CollectionListFilter.New      ? Color.FromArgb("#23935D") : Colors.White;

    public Color AllFilterText      => SelectedFilter == CollectionListFilter.All      ? Colors.White : Color.FromArgb("#34473A");
    public Color OverdueFilterText  => SelectedFilter == CollectionListFilter.Overdue  ? Colors.White : Color.FromArgb("#34473A");
    public Color TodayFilterText    => SelectedFilter == CollectionListFilter.Today    ? Colors.White : Color.FromArgb("#34473A");
    public Color WeekFilterText     => SelectedFilter == CollectionListFilter.ThisWeek ? Colors.White : Color.FromArgb("#34473A");
    public Color UpcomingFilterText => SelectedFilter == CollectionListFilter.Upcoming ? Colors.White : Color.FromArgb("#34473A");
    public Color NewFilterText      => SelectedFilter == CollectionListFilter.New      ? Colors.White : Color.FromArgb("#34473A");

    public CollectionsViewModel(
        GetCollectionsDashboardUseCase getDashboard,
        GetCollectionsUseCase getCollections,
        GetRecentCollectorPaymentsUseCase getRecentPayments,
        ICollectorDebtService debtService)
    {
        _getDashboard = getDashboard;
        _getCollections = getCollections;
        _getRecentPayments = getRecentPayments;
        _debtService = debtService;

        _ = LoadAllAsync();
    }

    public Task RefreshAsync(CancellationToken ct = default) => LoadAllAsync(ct);

    [RelayCommand]
    Task ShowAllAsync() => SetFilterAsync(CollectionListFilter.All);

    [RelayCommand]
    Task ShowOverdueAsync() => SetFilterAsync(CollectionListFilter.Overdue);

    [RelayCommand]
    Task ShowTodayAsync() => SetFilterAsync(CollectionListFilter.Today);

    [RelayCommand]
    Task ShowThisWeekAsync() => SetFilterAsync(CollectionListFilter.ThisWeek);

    [RelayCommand]
    Task ShowUpcomingAsync() => SetFilterAsync(CollectionListFilter.Upcoming);

    [RelayCommand]
    Task ShowNewAsync() => SetFilterAsync(CollectionListFilter.New);

    public Task ApplyFilterAsync(CollectionListFilter filter) => SetFilterAsync(filter);

    [RelayCommand]
    async Task SelectDebtAsync(CollectionDebtItem debt)
    {
        var statusColor = debt.Status switch
        {
            "Vencido" => Color.FromArgb("#F04438"),
            "Vence hoy" => Color.FromArgb("#F79009"),
            "Programado" => Color.FromArgb("#2D8CFF"),
            "Al corriente" => Color.FromArgb("#12B76A"),
            _ => Color.FromArgb("#667085")
        };

        var page = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
        if (page.BindingContext is RegisterPaymentViewModel vm)
        {
            vm.Load(new RegisterPaymentSnapshot(
                ClientId: debt.ClientId,
                ClientName: debt.ClientName,
                ClientNameUpper: debt.ClientName.ToUpperInvariant(),
                StatusLabel: debt.Status,
                StatusColor: statusColor,
                DebtId: debt.DebtId));
        }

        var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
    }

    [RelayCommand]
    async Task EditDebtAsync(CollectionDebtItem debt)
    {
        try
        {
            var detail = await _debtService.GetDebtAsync(debt.ClientId, debt.DebtId);
            var page = MauiProgram.Services.GetRequiredService<CreateDebtPage>();
            if (page.BindingContext is CreateDebtViewModel vm)
            {
                vm.LoadForEdit(detail, debt.ClientName);
                vm.OnDebtCreated = async () => await RefreshAsync();
            }

            var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (nav is not null)
                await nav.Navigation.PushAsync(page);
        }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo cargar la deuda", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error", "No pudimos cargar los datos de la deuda para editarla.");
        }
    }

    [RelayCommand]
    async Task NewDebtAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<CreateDebtPage>();
        if (page.BindingContext is CreateDebtViewModel vm)
            vm.ResetForDirectCreation();

        var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
    }

    private async Task LoadAllAsync(CancellationToken externalCt = default)
    {
        CancelAndDispose(ref _reloadCts);
        _reloadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _reloadCts.Token;

        IsLoading = true;

        try
        {
            var dashboardTask = _getDashboard.ExecuteAsync(ct);
            var listTask      = _getCollections.ExecuteAsync(ToApiFilter(SelectedFilter), ct);
            var recentTask    = _getRecentPayments.ExecuteAsync(4, ct);

            await Task.WhenAll(dashboardTask, listTask, recentTask);

            ApplyDashboard(dashboardTask.Result);
            ApplySections(listTask.Result);
            ApplyRecentPayments(recentTask.Result);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudieron cargar los cobros", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos cargar el centro de cobranza. Verifica tu conexión e intenta de nuevo.");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasSections));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasRecentPayments));
            OnPropertyChanged(nameof(IsRecentPaymentsEmpty));
        }
    }

    private async Task SetFilterAsync(CollectionListFilter filter)
    {
        if (SelectedFilter == filter)
            return;

        SelectedFilter = filter;
        await LoadSectionsAsync();
    }

    private async Task LoadSectionsAsync()
    {
        CancelAndDispose(ref _sectionsCts);
        _sectionsCts = new CancellationTokenSource();
        var ct = _sectionsCts.Token;

        IsLoading = true;

        try
        {
            var response = await _getCollections.ExecuteAsync(ToApiFilter(SelectedFilter), ct);
            ApplySections(response);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudieron actualizar los cobros", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos actualizar la vista operativa en este momento.");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasSections));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void ApplyDashboard(CollectorCollectionsDashboardResponse response)
    {
        TotalAdeudado = FormatMoney(response.Portfolio.TotalOutstandingAmount);
        TotalDeudas   = $"{response.Portfolio.TotalActiveDebtsCount} deuda{(response.Portfolio.TotalActiveDebtsCount == 1 ? "" : "s")}";
        VencidasCount  = response.Portfolio.OverdueInstallmentsCount.ToString();
        VencidasAmount = FormatMoney(response.Portfolio.TotalOverdueAmount);

        CobrosHoyCount    = $"{response.Today.ItemsCount} cobro{(response.Today.ItemsCount == 1 ? "" : "s")}";
        CobrosHoyAmount   = FormatMoney(response.Today.TotalAmount);
        CobrosSemanaCount  = $"{response.Week.ItemsCount} cobro{(response.Week.ItemsCount == 1 ? "" : "s")}";
        CobrosSemanaAmount = FormatMoney(response.Week.TotalAmount);

        var ct = response.CollectedToday;
        var cw = response.CollectedWeek;
        CobradoHoyCount    = $"{ct?.ItemsCount ?? 0} pago{((ct?.ItemsCount ?? 0) == 1 ? "" : "s")}";
        CobradoHoyAmount   = FormatMoney(ct?.TotalAmount ?? 0m);
        CobradoSemanaCount  = $"{cw?.ItemsCount ?? 0} pago{((cw?.ItemsCount ?? 0) == 1 ? "" : "s")}";
        CobradoSemanaAmount = FormatMoney(cw?.TotalAmount ?? 0m);
    }

    private void ApplySections(CollectorCollectionsListResponse response)
    {
        Sections.Clear();
        foreach (var section in response.Sections ?? [])
            Sections.Add(MapToUiSection(section));

        VisibleCountSummary = response.Summary.ItemsCount == 0
            ? "Sin cobros para este filtro"
            : $"{response.Summary.ItemsCount} cobro{(response.Summary.ItemsCount == 1 ? "" : "s")} · {FormatMoney(response.Summary.TotalAmount)}";

        OnPropertyChanged(nameof(HasSections));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void ApplyRecentPayments(CollectorRecentPaymentsResponse response)
    {
        RecentPayments.Clear();
        foreach (var item in response.Items ?? [])
            RecentPayments.Add(new CollectionRecentPaymentItem(
                Initials: BuildInitials(item.ClientName),
                AvatarColor: AvatarColorFor(item.ClientName),
                ClientName: item.ClientName,
                AmountText: FormatMoney(item.Amount),
                MethodLabel: item.Method,
                RegisteredAtText: FormatTimestamp(item.RegisteredAt),
                NotesText: string.IsNullOrWhiteSpace(item.Notes) ? "Pago registrado sin notas." : item.Notes!.Trim()));

        RecentActivitySummary = RecentPayments.Count == 0
            ? "Aún no hay pagos recientes."
            : $"{RecentPayments.Count} movimiento{(RecentPayments.Count == 1 ? "" : "s")} reciente{(RecentPayments.Count == 1 ? "" : "s")}.";

        OnPropertyChanged(nameof(HasRecentPayments));
        OnPropertyChanged(nameof(IsRecentPaymentsEmpty));
    }

    private static CollectionDebtSection MapToUiSection(CollectorCollectionsSectionDto section)
        => new(
            section.Title,
            section.Subtitle,
            AccentForSectionKey(section.Key),
            (section.Items ?? []).Select(MapToUiItem));

    private static CollectionDebtItem MapToUiItem(CollectorCollectionItemDto item)
    {
        var (statusBg, statusText) = StatusColorsFor(item.StatusLabel);

        return new CollectionDebtItem(
            Initials: string.IsNullOrWhiteSpace(item.ClientInitials) ? BuildInitials(item.ClientName) : item.ClientInitials,
            AvatarColor: AvatarColorFor(item.ClientName),
            ClientId: item.ClientId,
            DebtId: item.DebtId,
            ClientName: item.ClientName,
            Description: $"{item.Description} — Cuota {item.InstallmentNumber}",
            DueDate: item.DueDate,
            Amount: FormatMoney(item.RemainingAmount),
            Status: item.StatusLabel,
            StatusBg: statusBg,
            StatusText: statusText,
            DueText: string.IsNullOrWhiteSpace(item.DueDateDisplay) ? FormatDueDate(item.DueDate) : item.DueDateDisplay,
            IsOverdueItem: item.IsOverdue,
            IsDueTodayItem: item.IsDueToday,
            IsInThisWeekItem: item.IsInThisWeek,
            HasInterest: item.HasMoratory,
            InterestLabel: item.HasMoratory ? $"Mora ({item.MoratoryRate:0.##}%)" : string.Empty,
            InterestAmount: item.HasMoratory ? $"+ {FormatMoney(item.MoratoryAmount)}" : string.Empty,
            TotalAmount: item.HasMoratory ? FormatMoney(item.TotalDueAmount) : string.Empty,
            HasPartialPayment: item.HasPartialPayment);
    }

    private static (Color Bg, Color Text) StatusColorsFor(string statusLabel) => statusLabel switch
    {
        "Vencido" => (Color.FromArgb("#FFF1F3"), Color.FromArgb("#F04438")),
        "Vence hoy" => (Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708")),
        "Programado" => (Color.FromArgb("#EEF4FF"), Color.FromArgb("#2D68FF")),
        "Al corriente" => (Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48")),
        _ => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#667085"))
    };

    private static Color AccentForSectionKey(string key) => key?.Trim().ToLowerInvariant() switch
    {
        "overdue" => Color.FromArgb("#F04438"),
        "today" => Color.FromArgb("#F79009"),
        "rest_of_week" => Color.FromArgb("#2D8CFF"),
        "this_week" => Color.FromArgb("#2D8CFF"),
        "upcoming" => Color.FromArgb("#12B76A"),
        _ => Color.FromArgb("#98A2B3")
    };

    private static string ToApiFilter(CollectionListFilter filter) => filter switch
    {
        CollectionListFilter.Overdue  => "overdue",
        CollectionListFilter.Today    => "today",
        CollectionListFilter.ThisWeek => "this_week",
        CollectionListFilter.Upcoming => "upcoming",
        CollectionListFilter.New      => "new",
        _                             => "all"
    };

    private static string FormatMoney(decimal amount) => $"${amount:N2}";

    private static string FormatDueDate(DateTime dueDate)
        => $"Vence {dueDate:dd MMM}".ToLowerInvariant().Replace("ene", "ene").Replace("apr", "abr").Replace("aug", "ago").Replace("dec", "dic");

    private static string FormatTimestamp(DateTime registeredAt)
    {
        var local = registeredAt.Kind == DateTimeKind.Utc ? registeredAt.ToLocalTime() : registeredAt;
        if (local.Date == DateTime.Today)
            return $"Hoy · {local:hh:mm tt}".ToLowerInvariant();
        if (local.Date == DateTime.Today.AddDays(-1))
            return $"Ayer · {local:hh:mm tt}".ToLowerInvariant();

        return local.ToString("dd MMM · hh:mm tt").ToLowerInvariant();
    }

    private static string BuildInitials(string name)
    {
        var parts = (name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(x => char.ToUpperInvariant(x[0]));

        var initials = string.Concat(parts);
        return string.IsNullOrWhiteSpace(initials) ? "CL" : initials;
    }

    private static Color AvatarColorFor(string name)
    {
        ReadOnlySpan<string> palette = ["#2563EB", "#D97706", "#DC2626", "#0D9488",
                                        "#166534", "#7C3AED", "#DB2777", "#059669"];
        var index = Math.Abs(name.GetHashCode()) % palette.Length;
        return Color.FromArgb(palette[index]);
    }

    private static void CancelAndDispose(ref CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            cts.Cancel();
        }
        catch { }

        cts.Dispose();
        cts = null;
    }

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }
}

public enum CollectionListFilter
{
    All,
    Overdue,
    Today,
    ThisWeek,
    Upcoming,
    New
}
