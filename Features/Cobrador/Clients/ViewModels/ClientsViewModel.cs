using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Features.Cobrador.Clients.AddClient.Views;
using Paynest.Features.Cobrador.Clients.Detail.ViewModels;
using Paynest.Features.Cobrador.Clients.Detail.Views;
using Paynest.Features.Cobrador.Clients.Models;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Features.Cobrador.Clients.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly ICollectorClientService _clientService;
    private List<ClientSummary> _allClients = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsAllSelected), nameof(IsPendienteSelected),
        nameof(IsVencidoSelected), nameof(IsAlDiaSelected), nameof(IsParcialSelected))]
    private string _selectedFilter = "Todos";

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasClients), nameof(IsEmpty))]
    private bool _isLoading;

    public bool HasClients => !IsLoading && Clients.Count > 0;
    public bool IsEmpty    => !IsLoading && Clients.Count == 0;

    public bool IsAllSelected       => SelectedFilter == "Todos";
    public bool IsPendienteSelected => SelectedFilter == "Pendiente";
    public bool IsVencidoSelected   => SelectedFilter == "Vencido";
    public bool IsAlDiaSelected     => SelectedFilter == "Al día";
    public bool IsParcialSelected   => SelectedFilter == "Parcial";

    public ObservableCollection<ClientSummary> Clients { get; } = [];

    public ClientsViewModel(ICollectorClientService clientService)
    {
        _clientService = clientService;
        _ = LoadAsync();
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var response = await _clientService.GetClientsAsync(ct);
            _allClients = response.Items.Select(MapToSummary).ToList();
            ApplyFilter();
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudieron cargar los clientes", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos cargar los clientes. Verifica tu conexión e intenta de nuevo.");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasClients));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value)     => ApplyFilter();

    [RelayCommand] void SelectFilter(string filter) => SelectedFilter = filter;

    [RelayCommand]
    async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    async Task AddClientAsync()
    {
        var page = MauiProgram.Services.GetRequiredService<AddClientPage>();

        if (Shell.Current is not null)
        {
            await Shell.Current.Navigation.PushAsync(page);
            return;
        }

        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page currentPage)
            await currentPage.Navigation.PushAsync(page);
    }

    [RelayCommand]
    async Task OpenClientDetailAsync(ClientSummary client)
    {
        var page = MauiProgram.Services.GetRequiredService<ClientDetailPage>();
        if (page.BindingContext is ClientDetailViewModel vm)
            vm.Load(client);

        if (Shell.Current is not null)
        {
            await Shell.Current.Navigation.PushAsync(page);
            return;
        }

        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page currentPage)
            await currentPage.Navigation.PushAsync(page);
    }

    private void ApplyFilter()
    {
        IEnumerable<ClientSummary> results = _allClients;

        if (SelectedFilter != "Todos")
            results = results.Where(c => c.Status == SelectedFilter);

        if (!string.IsNullOrWhiteSpace(SearchText))
            results = results.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Clients.Clear();
        foreach (var item in results)
            Clients.Add(item);

        OnPropertyChanged(nameof(HasClients));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static ClientSummary MapToSummary(CollectorClientSummaryDto dto)
    {
        var (statusBg, statusText) = dto.Status switch
        {
            "Vencido"   => (Color.FromArgb("#FFEBEE"), Color.FromArgb("#D32F2F")),
            "Pendiente" => (Color.FromArgb("#FFF3E0"), Color.FromArgb("#E65100")),
            "Al día"    => (Color.FromArgb("#E8F5E9"), Color.FromArgb("#2E7D32")),
            "Parcial"   => (Color.FromArgb("#E3F2FD"), Color.FromArgb("#1565C0")),
            _           => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#667085"))
        };

        return new ClientSummary(
            Id:              dto.ClientId,
            Name:            dto.Name,
            Initials:        dto.Initials,
            AvatarColor:     AvatarColorFor(dto.Name),
            DateText:        dto.NextDueDateDisplay,
            Amount:          $"${dto.OutstandingAmount:N2}",
            Status:          dto.Status,
            StatusBgColor:   statusBg,
            StatusTextColor: statusText);
    }

    private static Color AvatarColorFor(string name)
    {
        ReadOnlySpan<string> palette = ["#2563EB", "#D97706", "#DC2626", "#0D9488",
                                        "#166534", "#7C3AED", "#DB2777", "#059669"];
        var index = Math.Abs(name.GetHashCode()) % palette.Length;
        return Color.FromArgb(palette[index]);
    }

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }
}
