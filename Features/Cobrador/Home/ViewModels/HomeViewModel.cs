using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Core.Interfaces;
using Paynest.Infrastructure.Exceptions;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Home.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly AuthStateService _authState;
    private readonly ICollectorDashboardService _dashboardService;

    public string Greeting => DateTime.Now.Hour switch
    {
        < 12 => "Buenos días,",
        < 18 => "Buenas tardes,",
        _    => "Buenas noches,"
    };

    public string UserName => _authState.CurrentUser?.FirstName ?? "Usuario";

    [ObservableProperty] private string _cobradoHoy    = "—";
    [ObservableProperty] private string _deltaHoy      = string.Empty;
    [ObservableProperty] private string _estaSemana    = "—";
    [ObservableProperty] private string _metaSemanal   = string.Empty;
    [ObservableProperty] private string _pendienteTotal    = "—";
    [ObservableProperty] private string _pendienteClientes = string.Empty;
    [ObservableProperty] private string _vencidoTotal      = "—";
    [ObservableProperty] private string _vencidoClientes   = string.Empty;

    public HomeViewModel(AuthStateService authState, ICollectorDashboardService dashboardService)
    {
        _authState        = authState;
        _dashboardService = dashboardService;
        _ = LoadAsync();
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var stats = await _dashboardService.GetDashboardAsync(ct);

            var arrow  = stats.DeltaIsPositive ? "↑" : "↓";
            var pct    = Math.Abs(stats.DeltaVsYesterdayPercent);

            CobradoHoy        = $"${stats.CollectedToday:N2}";
            DeltaHoy          = $"{arrow} {pct:0.#}% vs ayer";
            EstaSemana        = $"${stats.CollectedThisWeek:N2}";
            MetaSemanal       = $"de ${stats.WeeklyGoal:N2} meta";
            PendienteTotal    = $"${stats.TotalPending:N2}";
            PendienteClientes = $"{stats.PendingClientsCount} cliente{(stats.PendingClientsCount == 1 ? "" : "s")}";
            VencidoTotal      = $"${stats.TotalOverdue:N2}";
            VencidoClientes   = $"{stats.OverdueClientsCount} cliente{(stats.OverdueClientsCount == 1 ? "" : "s")}";
        }
        catch (OperationCanceledException) { }
        catch (ApiException) { }
        catch (Exception)   { }
    }

    [RelayCommand]
    async Task NuevoCobroAsync()
        => await Shell.Current.GoToAsync("//collections");

    [RelayCommand]
    async Task AnadirClienteAsync()
        => await Shell.Current.GoToAsync("//clients");

    [RelayCommand]
    async Task TicketAsync()
        => await Shell.Current.GoToAsync("//collections");

    [RelayCommand]
    async Task VerAgendaAsync()
        => await Shell.Current.GoToAsync("//schedule");

    [RelayCommand]
    async Task VerDeudasAsync()
        => await Shell.Current.GoToAsync("//clients");

    [RelayCommand]
    async Task NotificacionesAsync()
    {
    }
}
