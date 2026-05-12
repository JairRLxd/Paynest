using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Models.Cobrador.Clients;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.Pages;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    private readonly GetClientListUseCase _getClientList;
    private List<ClientSummary> _allClients = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsAllSelected), nameof(IsPendienteSelected),
        nameof(IsVencidoSelected), nameof(IsAlDiaSelected), nameof(IsParcialSelected),
        nameof(AllFilterBg), nameof(PendienteFilterBg), nameof(VencidoFilterBg), nameof(AlDiaFilterBg), nameof(ParcialFilterBg),
        nameof(AllFilterText), nameof(PendienteFilterText), nameof(VencidoFilterText), nameof(AlDiaFilterText), nameof(ParcialFilterText))]
    private string _selectedFilter = "Todos";

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _visibleCountSummary = string.Empty;

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

    public Color AllFilterBg       => IsAllSelected ? Color.FromArgb("#23935D") : Colors.White;
    public Color PendienteFilterBg => IsPendienteSelected ? Color.FromArgb("#23935D") : Colors.White;
    public Color VencidoFilterBg   => IsVencidoSelected ? Color.FromArgb("#23935D") : Colors.White;
    public Color AlDiaFilterBg     => IsAlDiaSelected ? Color.FromArgb("#23935D") : Colors.White;
    public Color ParcialFilterBg   => IsParcialSelected ? Color.FromArgb("#23935D") : Colors.White;

    public Color AllFilterText       => IsAllSelected ? Colors.White : Color.FromArgb("#34473A");
    public Color PendienteFilterText => IsPendienteSelected ? Colors.White : Color.FromArgb("#34473A");
    public Color VencidoFilterText   => IsVencidoSelected ? Colors.White : Color.FromArgb("#34473A");
    public Color AlDiaFilterText     => IsAlDiaSelected ? Colors.White : Color.FromArgb("#34473A");
    public Color ParcialFilterText   => IsParcialSelected ? Colors.White : Color.FromArgb("#34473A");

    public ObservableCollection<ClientSummary> Clients { get; } = [];

    public ClientsViewModel(GetClientListUseCase getClientList)
    {
        _getClientList = getClientList;
        _ = LoadAsync();
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var response = await _getClientList.ExecuteAsync(ct);
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

    public Task RefreshAsync(CancellationToken ct = default) => LoadAsync(ct);

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

        VisibleCountSummary = Clients.Count == 0
            ? "Sin clientes para este filtro"
            : $"{Clients.Count} cliente{(Clients.Count == 1 ? "" : "s")} visibles";

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
