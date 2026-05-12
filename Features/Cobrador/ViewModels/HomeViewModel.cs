using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.Pages;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;
using Paynest.Features.Cobrador;

namespace Paynest.Features.Cobrador.ViewModels;

public sealed record HomeTodayVisitItem(
    string ClientName,
    string ClientInitials,
    Color AvatarColor,
    string InstallmentLabel,
    string AmountText,
    string StatusLabel,
    Color StatusBg,
    Color StatusFg,
    string ClientId,
    string DebtId,
    string InstallmentId);

public sealed record HomeRecentPaymentItem(
    string ClientInitials,
    Color AvatarColor,
    string ClientName,
    string AmountText,
    string MethodText,
    string TimeAgoText);

public partial class HomeViewModel : ObservableObject
{
    private readonly AuthStateService                    _authState;
    private readonly GetDashboardUseCase                 _getDashboard;
    private readonly GetAgendaDayUseCase                 _getAgendaDay;
    private readonly GetRecentCollectorPaymentsUseCase   _getRecentPayments;

    public string Greeting => DateTime.Now.Hour switch
    {
        < 12 => "Buenos días,",
        < 18 => "Buenas tardes,",
        _    => "Buenas noches,"
    };

    public string UserName => _authState.CurrentUser?.FirstName ?? "Usuario";

    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _isTodayLoading;
    [ObservableProperty] private bool   _isRecentLoading;
    [ObservableProperty] private string _cobradoHoy          = "—";
    [ObservableProperty] private string _deltaHoy            = string.Empty;
    [ObservableProperty] private bool   _deltaIsPositive     = true;
    [ObservableProperty] private string _estaSemana          = "—";
    [ObservableProperty] private string _metaSemanal         = string.Empty;
    [ObservableProperty] private double _weeklyProgress      = 0;
    [ObservableProperty] private string _pendienteTotal      = "—";
    [ObservableProperty] private string _pendienteClientes   = string.Empty;
    [ObservableProperty] private string _vencidoTotal        = "—";
    [ObservableProperty] private string _vencidoClientes     = string.Empty;
    [ObservableProperty] private bool   _hasMorToday;

    public ObservableCollection<HomeTodayVisitItem>   TodayItems   { get; } = [];
    public ObservableCollection<HomeRecentPaymentItem> RecentItems { get; } = [];

    public bool HasTodayItems   => !IsTodayLoading  && TodayItems.Count > 0;
    public bool IsTodayEmpty    => !IsTodayLoading  && TodayItems.Count == 0;
    public bool HasRecentItems  => !IsRecentLoading && RecentItems.Count > 0;
    public bool IsRecentEmpty   => !IsRecentLoading && RecentItems.Count == 0;
    public string DeltaArrow    => DeltaIsPositive ? "↑" : "↓";
    public Color DeltaColor     => DeltaIsPositive ? Color.FromArgb("#B8DCC9") : Color.FromArgb("#FFBBBB");

    public HomeViewModel(
        AuthStateService authState,
        GetDashboardUseCase getDashboard,
        GetAgendaDayUseCase getAgendaDay,
        GetRecentCollectorPaymentsUseCase getRecentPayments)
    {
        _authState         = authState;
        _getDashboard      = getDashboard;
        _getAgendaDay      = getAgendaDay;
        _getRecentPayments = getRecentPayments;
        _ = LoadAsync();
    }

    public Task RefreshAsync(CancellationToken ct = default) => LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading) return;
        IsLoading       = true;
        IsTodayLoading  = true;
        IsRecentLoading = true;
        OnPropertyChanged(nameof(HasTodayItems));
        OnPropertyChanged(nameof(IsTodayEmpty));
        OnPropertyChanged(nameof(HasRecentItems));
        OnPropertyChanged(nameof(IsRecentEmpty));

        await Task.WhenAll(
            LoadDashboardAsync(ct),
            LoadTodayAsync(ct),
            LoadRecentAsync(ct));

        IsLoading = false;
    }

    private async Task LoadDashboardAsync(CancellationToken ct)
    {
        try
        {
            var stats = await _getDashboard.ExecuteAsync(ct);

            DeltaIsPositive = stats.DeltaIsPositive;
            var pct = Math.Abs(stats.DeltaVsYesterdayPercent);

            CobradoHoy        = $"${stats.CollectedToday:N2}";
            DeltaHoy          = $"{pct:0.#}% vs ayer";
            EstaSemana        = $"${stats.CollectedThisWeek:N2}";
            MetaSemanal       = $"de ${stats.WeeklyGoal:N2} meta";
            WeeklyProgress    = stats.WeeklyGoal > 0
                ? Math.Min(1.0, (double)(stats.CollectedThisWeek / stats.WeeklyGoal))
                : 0;
            PendienteTotal    = $"${stats.TotalPending:N2}";
            PendienteClientes = $"{stats.PendingClientsCount} cliente{(stats.PendingClientsCount == 1 ? "" : "s")}";
            VencidoTotal      = $"${stats.TotalOverdue:N2}";
            VencidoClientes   = $"{stats.OverdueClientsCount} cliente{(stats.OverdueClientsCount == 1 ? "" : "s")}";

            OnPropertyChanged(nameof(DeltaArrow));
            OnPropertyChanged(nameof(DeltaColor));
        }
        catch (OperationCanceledException) { }
        catch (ApiException) { }
        catch (Exception) { }
        finally { IsLoading = false; }
    }

    private async Task LoadTodayAsync(CancellationToken ct)
    {
        try
        {
            var response = await _getAgendaDay.ExecuteAsync(DateTime.Today, ct: ct);
            TodayItems.Clear();
            var items = response.Items ?? [];
            HasMorToday = items.Count > 3;
            foreach (var item in items.Take(3))
            {
                var (bg, fg) = item.IsOverdue
                    ? (Color.FromArgb("#FFF1F3"), Color.FromArgb("#F04438"))
                    : item.Status.Contains("due_today", StringComparison.OrdinalIgnoreCase)
                        ? (Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708"))
                        : (Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48"));

                TodayItems.Add(new HomeTodayVisitItem(
                    ClientName:       item.ClientName,
                    ClientInitials:   BuildInitials(item.ClientName),
                    AvatarColor:      AvatarColorFor(item.ClientName),
                    InstallmentLabel: $"Cuota {item.InstallmentNumber} · {item.Description}",
                    AmountText:       $"${item.AmountDue:N2}",
                    StatusLabel:      item.StatusLabel,
                    StatusBg:         bg,
                    StatusFg:         fg,
                    ClientId:         item.ClientId,
                    DebtId:           item.DebtId,
                    InstallmentId:    item.InstallmentId));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
        finally
        {
            IsTodayLoading = false;
            OnPropertyChanged(nameof(HasTodayItems));
            OnPropertyChanged(nameof(IsTodayEmpty));
        }
    }

    private async Task LoadRecentAsync(CancellationToken ct)
    {
        try
        {
            var response = await _getRecentPayments.ExecuteAsync(limit: 3, ct);
            RecentItems.Clear();
            foreach (var item in response.Items ?? [])
            {
                RecentItems.Add(new HomeRecentPaymentItem(
                    ClientInitials: BuildInitials(item.ClientName),
                    AvatarColor:    AvatarColorFor(item.ClientName),
                    ClientName:     item.ClientName,
                    AmountText:     $"${item.Amount:N2}",
                    MethodText:     ToMethodLabel(item.Method),
                    TimeAgoText:    ToTimeAgo(item.RegisteredAt)));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
        finally
        {
            IsRecentLoading = false;
            OnPropertyChanged(nameof(HasRecentItems));
            OnPropertyChanged(nameof(IsRecentEmpty));
        }
    }

    // ── Comandos ─────────────────────────────────────────────────────────

    [RelayCommand]
    async Task NuevaDeudaAsync()
    {
        var picker = MauiProgram.Services.GetRequiredService<ClientPickerPage>();
        if (picker.BindingContext is ClientPickerViewModel pickerVm)
        {
            pickerVm.OnClientSelected = async client =>
            {
                var debtPage = MauiProgram.Services.GetRequiredService<CreateDebtPage>();
                if (debtPage.BindingContext is CreateDebtViewModel debtVm)
                    debtVm.LoadClientContext(client);

                var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (nav is not null)
                    await nav.Navigation.PushAsync(debtPage);
            };
        }

        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (root is not null)
            await root.Navigation.PushAsync(picker);
    }

    [RelayCommand]
    async Task RegistrarPagoAsync()
    {
        var picker = MauiProgram.Services.GetRequiredService<ClientPickerPage>();
        if (picker.BindingContext is ClientPickerViewModel pickerVm)
        {
            pickerVm.OnClientSelected = async client =>
            {
                var payPage = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
                if (payPage.BindingContext is RegisterPaymentViewModel payVm)
                    payVm.Load(new RegisterPaymentSnapshot(
                        ClientId:        client.Id,
                        ClientName:      client.Name,
                        ClientNameUpper: client.Name.ToUpperInvariant(),
                        StatusLabel:     client.Status,
                        StatusColor:     client.StatusTextColor));

                var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (nav is not null)
                    await nav.Navigation.PushAsync(payPage);
            };
        }

        var root = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (root is not null)
            await root.Navigation.PushAsync(picker);
    }

    [RelayCommand]
    async Task AnadirClienteAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<AddClientPage>();
        var nav  = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
    }

    [RelayCommand]
    Task VerCobroAsync()
        => Shell.Current.GoToAsync("//collections");

    [RelayCommand]
    Task EditarDeudaAsync()
    {
        CollectionsPendingFilter.Request(CollectionListFilter.New);
        return Shell.Current.GoToAsync("//collections");
    }

    [RelayCommand]
    Task VerAgendaAsync()
        => Shell.Current.GoToAsync("//schedule");

    [RelayCommand]
    Task VerDeudasAsync()
        => Shell.Current.GoToAsync("//clients");

    [RelayCommand]
    async Task AbrirCobroAsync(HomeTodayVisitItem? item)
    {
        if (item is null) return;

        var page = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
        if (page.BindingContext is RegisterPaymentViewModel vm)
            vm.Load(new RegisterPaymentSnapshot(
                ClientId:        item.ClientId,
                ClientName:      item.ClientName,
                ClientNameUpper: item.ClientName.ToUpperInvariant(),
                StatusLabel:     item.StatusLabel,
                StatusColor:     item.StatusFg,
                DebtId:          item.DebtId,
                InstallmentId:   item.InstallmentId));

        var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
    }

    [RelayCommand]
    async Task NotificacionesAsync()
        => await Shell.Current.GoToAsync(nameof(NotificationsPage));

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string ToMethodLabel(string method) => method.Trim().ToLowerInvariant() switch
    {
        "wallet" or "paynestwallet" or "paynest_wallet" => "Saldo Paynest",
        "cash"     => "Efectivo",
        "transfer" => "Transferencia",
        "card"     => "Tarjeta",
        _ => string.IsNullOrWhiteSpace(method) ? "Pago" : method
    };

    private static string ToTimeAgo(DateTime dt)
    {
        var diff = DateTime.Now - dt;
        if (diff.TotalMinutes < 1)  return "Ahora";
        if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24)   return $"Hace {(int)diff.TotalHours} h";
        if (diff.TotalDays < 2)     return "Ayer";
        if (diff.TotalDays < 7)     return $"Hace {(int)diff.TotalDays} días";
        return dt.ToString("d MMM");
    }

    private static string BuildInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "CL";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
    }

    private static Color AvatarColorFor(string name)
    {
        ReadOnlySpan<string> palette = ["#2563EB", "#D97706", "#DC2626", "#0D9488",
                                        "#166534", "#7C3AED", "#DB2777", "#059669"];
        return Color.FromArgb(palette[Math.Abs(name.GetHashCode()) % palette.Length]);
    }
}
