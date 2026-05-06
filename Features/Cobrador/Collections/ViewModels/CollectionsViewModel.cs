using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Interfaces;
using Paynest.Core.Models.Cobrador.Collections;
using Paynest.Features.Cobrador.Clients.CreateDebt.ViewModels;
using Paynest.Features.Cobrador.Clients.CreateDebt.Views;
using Paynest.Features.Cobrador.Clients.Models;
using Paynest.Features.Cobrador.Clients.RegisterPayment.ViewModels;
using Paynest.Features.Cobrador.Clients.RegisterPayment.Views;
using Paynest.Features.Cobrador.Collections.Models;
using Paynest.Infrastructure.Exceptions;
using System.Collections.ObjectModel;

namespace Paynest.Features.Cobrador.Collections.ViewModels;

public partial class CollectionsViewModel : ObservableObject
{
    private readonly ICollectorCollectionsService _collectionsService;

    // ── Estadísticas ───────────────────────────────────────────────────────

    [ObservableProperty] private string _totalAdeudado  = "—";
    [ObservableProperty] private string _totalDeudas    = "—";
    [ObservableProperty] private string _vencidasCount  = "—";
    [ObservableProperty] private string _vencidasAmount = "—";

    // ── Estado de UI ───────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDebts), nameof(IsEmpty))]
    private bool _isLoading;

    public bool HasDebts => !IsLoading && Debts.Count > 0;
    public bool IsEmpty  => !IsLoading && Debts.Count == 0;

    // ── Lista de cobros ────────────────────────────────────────────────────

    public ObservableCollection<CollectionDebtItem> Debts { get; } = [];

    public CollectionsViewModel(ICollectorCollectionsService collectionsService)
    {
        _collectionsService = collectionsService;
        _ = LoadAsync();
    }

    // ── Carga desde backend ────────────────────────────────────────────────

    private async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Debts.Clear();
        OnPropertyChanged(nameof(HasDebts));
        OnPropertyChanged(nameof(IsEmpty));

        try
        {
            var response = await _collectionsService.GetCollectionsAsync(ct);
            ApplyResponse(response);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudieron cargar los cobros", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos cargar los cobros. Verifica tu conexión e intenta de nuevo.");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasDebts));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void ApplyResponse(CollectorCollectionsResponse response)
    {
        TotalAdeudado  = FormatMoney(response.TotalOutstandingAmount);
        TotalDeudas    = $"{response.TotalActiveDebtsCount} deuda{(response.TotalActiveDebtsCount == 1 ? "" : "s")}";
        VencidasCount  = response.OverdueInstallmentsCount.ToString();
        VencidasAmount = FormatMoney(response.TotalOverdueAmount);

        Debts.Clear();
        foreach (var item in response.Items ?? [])
            Debts.Add(MapToUiItem(item));
    }

    private static CollectionDebtItem MapToUiItem(CollectorCollectionItemDto item)
    {
        var (statusBg, statusText) = item.StatusLabel switch
        {
            "Atrasado"     => (Color.FromArgb("#FFF1F3"), Color.FromArgb("#F04438")),
            "Pendiente"    => (Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708")),
            "Al corriente" => (Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48")),
            "Liquidado"    => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#667085")),
            _              => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#667085"))
        };

        var installmentLabel = $"Cuota {item.InstallmentNumber}";
        var dueLine = item.IsOverdue
            ? $"Vencida · {item.DueDateDisplay}"
            : item.DueDateDisplay;

        return new CollectionDebtItem(
            Initials:       item.ClientInitials,
            AvatarColor:    AvatarColorFor(item.ClientName),
            ClientId:       item.ClientId,
            DebtId:         item.DebtId,
            ClientName:     item.ClientName,
            Description:    $"{item.Description} — {installmentLabel}",
            Amount:         FormatMoney(item.RemainingAmount),
            Status:         item.StatusLabel,
            StatusBg:       statusBg,
            StatusText:     statusText,
            DueText:        dueLine,
            HasInterest:    item.HasMoratory,
            InterestLabel:  item.HasMoratory ? $"Mora ({item.MoratoryRate:0.##}%)" : string.Empty,
            InterestAmount: item.HasMoratory ? $"+ {FormatMoney(item.MoratoryAmount)}" : string.Empty,
            TotalAmount:    item.HasMoratory ? FormatMoney(item.TotalDueAmount) : string.Empty);
    }

    // ── Comandos ───────────────────────────────────────────────────────────

    [RelayCommand]
    async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    async Task SelectDebtAsync(CollectionDebtItem debt)
    {
        var statusColor = debt.Status switch
        {
            "Atrasado"     => Color.FromArgb("#F04438"),
            "Pendiente"    => Color.FromArgb("#F79009"),
            "Al corriente" => Color.FromArgb("#12B76A"),
            "Liquidado"    => Color.FromArgb("#667085"),
            _              => Color.FromArgb("#667085")
        };

        var page = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
        if (page.BindingContext is RegisterPaymentViewModel vm)
            vm.Load(new RegisterPaymentSnapshot(
                ClientId:        debt.ClientId,
                ClientName:      debt.ClientName,
                ClientNameUpper: debt.ClientName.ToUpperInvariant(),
                StatusLabel:     debt.Status,
                StatusColor:     statusColor,
                DebtId:          debt.DebtId));

        var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
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

    [RelayCommand]
    Task DeleteDebt(CollectionDebtItem item)
    {
        Debts.Remove(item);
        OnPropertyChanged(nameof(HasDebts));
        OnPropertyChanged(nameof(IsEmpty));
        return Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string FormatMoney(decimal amount) => $"${amount:N2}";

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
            await p.DisplayAlert(title, msg, "Entendido");
    }
}
