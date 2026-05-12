using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Paynest.Core.Models.Cobrador.Agenda;
using Paynest.Features.Cobrador.Models;
using Paynest.Features.Cobrador.Pages;
using Paynest.Features.Cobrador.UseCases;
using Paynest.Infrastructure.Exceptions;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private static readonly CultureInfo _mxCulture = new("es-MX");
    private readonly GetAgendaMonthUseCase _getAgendaMonth;
    private readonly GetAgendaDayUseCase _getAgendaDay;
    private readonly RescheduleAgendaUseCase _rescheduleAgenda;
    private readonly Dictionary<string, CollectorAgendaMonthResponse> _monthCache = [];
    private readonly Dictionary<DateOnly, CollectorAgendaDayResponse> _dayCache = [];
    private readonly Dictionary<DateOnly, CollectorAgendaDaySummaryDto> _monthDays = [];
    private CancellationTokenSource? _monthCts;
    private CancellationTokenSource? _dayCts;
    private DateTime _visibleMonth;
    private DateTime _selectedDate;
    private bool _hasExplicitDaySelection;

    [ObservableProperty] private string _monthTitle = string.Empty;
    [ObservableProperty] private bool _isMonthLoading;
    [ObservableProperty] private bool _isDayLoading;
    [ObservableProperty] private bool _isRescheduling;
    [ObservableProperty] private string _calendarErrorMessage = string.Empty;
    [ObservableProperty] private string _dayErrorMessage = string.Empty;
    [ObservableProperty] private string _selectedDayTitle = string.Empty;
    [ObservableProperty] private string _selectedDaySubtitle = string.Empty;
    [ObservableProperty] private string _selectedDayAmount = "—";
    [ObservableProperty] private bool _hasAnyMonthItems;

    public ObservableCollection<AgendaCalendarDayItem> CalendarDays { get; } = [];
    public ObservableCollection<AgendaCollectionItem> DayItems { get; } = [];

    public bool HasCalendarError => !string.IsNullOrWhiteSpace(CalendarErrorMessage);
    public bool HasDayError => !string.IsNullOrWhiteSpace(DayErrorMessage);
    public bool HasDayItems => !IsDayLoading && DayItems.Count > 0;
    public bool HasSelectedDay => _hasExplicitDaySelection;
    public bool ShowDayPrompt => !IsDayLoading && !HasDayError && !HasSelectedDay;
    public bool IsDayEmpty => HasSelectedDay && !IsDayLoading && !HasDayError && DayItems.Count == 0;
    public bool ShowMonthEmptyHint => !IsMonthLoading && !HasCalendarError && !HasAnyMonthItems;

    public ScheduleViewModel(GetAgendaMonthUseCase getAgendaMonth, GetAgendaDayUseCase getAgendaDay, RescheduleAgendaUseCase rescheduleAgenda)
    {
        _getAgendaMonth = getAgendaMonth;
        _getAgendaDay = getAgendaDay;
        _rescheduleAgenda = rescheduleAgenda;
        _visibleMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selectedDate = DateTime.Today;
        ResetDayPanel();
    }

    public Task RefreshAsync(CancellationToken ct = default)
    {
        InvalidateMonthCache(_visibleMonth);
        InvalidateDayCacheForVisibleMonth();
        return LoadMonthAsync(_visibleMonth, preserveSelection: HasSelectedDay, ct, preferCache: false);
    }

    [RelayCommand]
    Task PreviousMonthAsync()
        => LoadMonthAsync(_visibleMonth.AddMonths(-1), preserveSelection: false);

    [RelayCommand]
    Task NextMonthAsync()
        => LoadMonthAsync(_visibleMonth.AddMonths(1), preserveSelection: false);

    [RelayCommand]
    Task SelectDayAsync(AgendaCalendarDayItem? day)
    {
        if (day is null || !day.IsCurrentMonth)
            return Task.CompletedTask;

        if (_selectedDate.Date == day.Date.Date && DayItems.Count > 0 && !IsDayLoading)
            return Task.CompletedTask;

        _hasExplicitDaySelection = true;
        _selectedDate = day.Date.Date;
        UpdateCalendarSelectionOnly();
        OnPropertyChanged(nameof(HasSelectedDay));
        OnPropertyChanged(nameof(ShowDayPrompt));
        OnPropertyChanged(nameof(IsDayEmpty));
        return LoadDayAsync(_selectedDate);
    }

    [RelayCommand]
    async Task OpenRegisterPaymentAsync(AgendaCollectionItem? item)
    {
        if (item is null)
            return;

        var statusColor = item.IsOverdue
            ? Color.FromArgb("#F04438")
            : item.StatusLabel.Contains("hoy", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb("#F79009")
                : Color.FromArgb("#027A48");

        var page = MauiProgram.Services.GetRequiredService<RegisterPaymentPage>();
        if (page.BindingContext is RegisterPaymentViewModel vm)
            vm.Load(new RegisterPaymentSnapshot(
                ClientId: item.ClientId,
                ClientName: item.ClientName,
                ClientNameUpper: item.ClientName.ToUpperInvariant(),
                StatusLabel: item.StatusLabel,
                StatusColor: statusColor,
                DebtId: item.DebtId,
                InstallmentId: item.InstallmentId));

        var nav = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (nav is not null)
            await nav.Navigation.PushAsync(page);
    }

    public async Task RescheduleAsync(AgendaCollectionItem item, DateTime newDueDate, string? reason, CancellationToken ct = default)
    {
        IsRescheduling = true;
        try
        {
            await _rescheduleAgenda.ExecuteAsync(item.DebtId, item.InstallmentNumber, newDueDate, reason, ct);
            _dayCache.Remove(DateOnly.FromDateTime(_selectedDate));
            InvalidateMonthCache(_visibleMonth);
            await LoadMonthAsync(_visibleMonth, preserveSelection: true, ct, preferCache: false);
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            await ShowAlertAsync("No se pudo reprogramar", ex.Message);
        }
        catch (Exception)
        {
            await ShowAlertAsync("Error de conexión", "No pudimos reprogramar el cobro. Verifica tu conexión e intenta de nuevo.");
        }
        finally
        {
            IsRescheduling = false;
        }
    }

    private async Task LoadMonthAsync(DateTime month, bool preserveSelection, CancellationToken ct = default, bool preferCache = true)
    {
        CancelMonthRequest();
        IsMonthLoading = true;
        CalendarErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasCalendarError));

        _visibleMonth = new DateTime(month.Year, month.Month, 1);
        _monthCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var effectiveCt = _monthCts.Token;

        try
        {
            var monthKey = BuildMonthKey(_visibleMonth);
            CollectorAgendaMonthResponse response;
            if (preferCache && _monthCache.TryGetValue(monthKey, out var cachedMonth))
            {
                response = cachedMonth;
            }
            else
            {
                response = await _getAgendaMonth.ExecuteAsync(_visibleMonth.Year, _visibleMonth.Month, effectiveCt);
                _monthCache[monthKey] = response;
            }

            _monthDays.Clear();
            foreach (var day in response.Days ?? [])
                _monthDays[DateOnly.FromDateTime(day.Date)] = day;

            HasAnyMonthItems = _monthDays.Values.Any(x => x.ItemsCount > 0);
            ResolveSelectedDate(preserveSelection);
            BuildCalendar();
            if (HasSelectedDay)
                await LoadDayAsync(_selectedDate, effectiveCt);
            else
                ResetDayPanel();
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            _monthDays.Clear();
            HasAnyMonthItems = false;
            ResolveSelectedDate(preserveSelection);
            BuildCalendar();
            CalendarErrorMessage = ex.Message;
            OnPropertyChanged(nameof(HasCalendarError));
            ResetDayPanel();
            await ShowAlertAsync("No se pudo cargar la agenda", ex.Message);
        }
        catch (Exception)
        {
            _monthDays.Clear();
            HasAnyMonthItems = false;
            ResolveSelectedDate(preserveSelection);
            BuildCalendar();
            CalendarErrorMessage = "No pudimos cargar la agenda del mes. Verifica tu conexión e intenta de nuevo.";
            OnPropertyChanged(nameof(HasCalendarError));
            ResetDayPanel();
            await ShowAlertAsync("Error de conexión", CalendarErrorMessage);
        }
        finally
        {
            IsMonthLoading = false;
            OnPropertyChanged(nameof(ShowMonthEmptyHint));
            CancelMonthRequest(disposeOnly: true);
        }
    }

    private async Task LoadDayAsync(DateTime date, CancellationToken ct = default)
    {
        CancelDayRequest();
        IsDayLoading = true;
        DayErrorMessage = string.Empty;
        DayItems.Clear();
        UpdateSelectedDayHeader(date, null);
        OnPropertyChanged(nameof(HasDayError));
        OnPropertyChanged(nameof(HasDayItems));
        OnPropertyChanged(nameof(ShowDayPrompt));
        OnPropertyChanged(nameof(IsDayEmpty));
        _dayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var effectiveCt = _dayCts.Token;

        try
        {
            var dayKey = DateOnly.FromDateTime(date);
            CollectorAgendaDayResponse response;
            if (_dayCache.TryGetValue(dayKey, out var cachedDay))
            {
                response = cachedDay;
            }
            else
            {
                response = await _getAgendaDay.ExecuteAsync(date, ct: effectiveCt);
                _dayCache[dayKey] = response;
            }
            UpdateSelectedDayHeader(date, response);

            foreach (var item in response.Items ?? [])
                DayItems.Add(MapAgendaItem(item));
        }
        catch (OperationCanceledException) { }
        catch (ApiException ex)
        {
            UpdateSelectedDayHeader(date, null);
            DayErrorMessage = ex.Message;
            OnPropertyChanged(nameof(HasDayError));
            OnPropertyChanged(nameof(ShowDayPrompt));
            await ShowAlertAsync("No se pudo cargar el día", ex.Message);
        }
        catch (Exception)
        {
            UpdateSelectedDayHeader(date, null);
            DayErrorMessage = "No pudimos cargar los cobros del día. Verifica tu conexión e intenta de nuevo.";
            OnPropertyChanged(nameof(HasDayError));
            OnPropertyChanged(nameof(ShowDayPrompt));
            await ShowAlertAsync("Error de conexión", DayErrorMessage);
        }
        finally
        {
            IsDayLoading = false;
            OnPropertyChanged(nameof(HasDayItems));
            OnPropertyChanged(nameof(ShowDayPrompt));
            OnPropertyChanged(nameof(IsDayEmpty));
            CancelDayRequest(disposeOnly: true);
        }
    }

    private void ResolveSelectedDate(bool preserveSelection)
    {
        if (preserveSelection && _selectedDate.Year == _visibleMonth.Year && _selectedDate.Month == _visibleMonth.Month)
        {
            _selectedDate = _selectedDate.Date;
            return;
        }

        _hasExplicitDaySelection = false;

        var today = DateTime.Today;
        if (today.Year == _visibleMonth.Year && today.Month == _visibleMonth.Month)
        {
            _selectedDate = today;
            return;
        }

        var firstWithItems = _monthDays.Keys
            .Where(x => x.Year == _visibleMonth.Year && x.Month == _visibleMonth.Month)
            .OrderBy(x => x.DayNumber)
            .FirstOrDefault();

        _selectedDate = firstWithItems != default
            ? firstWithItems.ToDateTime(TimeOnly.MinValue)
            : _visibleMonth;
    }

    private void BuildCalendar()
    {
        CalendarDays.Clear();

        var firstDayOfMonth = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1);
        var startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstDayOfMonth.AddDays(-startOffset);

        MonthTitle = BuildMonthTitle(_visibleMonth);

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var isCurrentMonth = date.Month == _visibleMonth.Month;
            var isSelected = _hasExplicitDaySelection && date.Date == _selectedDate.Date;
            var isToday = date.Date == DateTime.Today;
            var summary = _monthDays.GetValueOrDefault(DateOnly.FromDateTime(date));
            var hasItems = (summary?.ItemsCount ?? 0) > 0;
            var hasOverdue = (summary?.OverdueCount ?? 0) > 0;
            var hasRescheduled = (summary?.RescheduledCount ?? 0) > 0;

            var background = isSelected
                ? Color.FromArgb("#2A6349")
                : isToday
                    ? Color.FromArgb("#F1F7F3")
                    : Colors.White;

            var border = isSelected
                ? Color.FromArgb("#2A6349")
                : isToday
                    ? Color.FromArgb("#97B7A5")
                    : Color.FromArgb("#E3E9E4");

            var dayTextColor = isSelected
                ? Colors.White
                : !isCurrentMonth
                    ? Color.FromArgb("#B9C2BC")
                    : Color.FromArgb("#1F2937");

            var badgeBackground = hasOverdue
                ? Color.FromArgb("#FFF1F3")
                : Color.FromArgb("#EEF4F0");

            var badgeText = hasOverdue
                ? Color.FromArgb("#F04438")
                : Color.FromArgb("#2A6349");

            CalendarDays.Add(new AgendaCalendarDayItem
            {
                Date = date,
                DayText = date.Day.ToString(),
                ItemsCountText = hasItems ? (summary!.ItemsCount > 9 ? "9+" : summary.ItemsCount.ToString()) : string.Empty,
                IsCurrentMonth = isCurrentMonth,
                IsSelected = isSelected,
                IsToday = isToday,
                HasItems = hasItems,
                HasOverdue = hasOverdue,
                HasRescheduled = hasRescheduled,
                BackgroundColor = background,
                BorderColor = border,
                DayTextColor = dayTextColor,
                ItemsBadgeBackground = badgeBackground,
                ItemsBadgeTextColor = badgeText,
                Opacity = isCurrentMonth ? 1 : 0.45
            });
        }
    }

    private void UpdateCalendarSelectionOnly()
    {
        foreach (var day in CalendarDays)
        {
            var isSelected = _hasExplicitDaySelection && day.Date.Date == _selectedDate.Date;
            day.IsSelected = isSelected;
            day.BackgroundColor = isSelected
                ? Color.FromArgb("#2A6349")
                : day.IsToday
                    ? Color.FromArgb("#F1F7F3")
                    : Colors.White;
            day.BorderColor = isSelected
                ? Color.FromArgb("#2A6349")
                : day.IsToday
                    ? Color.FromArgb("#97B7A5")
                    : Color.FromArgb("#E3E9E4");
            day.DayTextColor = isSelected
                ? Colors.White
                : !day.IsCurrentMonth
                    ? Color.FromArgb("#B9C2BC")
                    : Color.FromArgb("#1F2937");
        }
    }

    private void UpdateSelectedDayHeader(DateTime date, CollectorAgendaDayResponse? response)
    {
        var title = date.ToString("dddd d 'de' MMMM", _mxCulture);
        SelectedDayTitle = char.ToUpper(title[0], _mxCulture) + title[1..];

        if (response is null)
        {
            SelectedDaySubtitle = "Sin resumen disponible";
            SelectedDayAmount = "—";
            return;
        }

        var cobros = $"{response.TotalCount} cobro{(response.TotalCount == 1 ? "" : "s")}";
        var clientes = $"{response.ClientsCount} cliente{(response.ClientsCount == 1 ? "" : "s")}";
        SelectedDaySubtitle = $"{cobros} · {clientes}";
        SelectedDayAmount = FormatMoney(response.TotalAmount);
    }

    private static AgendaCollectionItem MapAgendaItem(CollectorAgendaItemDto item)
    {
        var (statusBackground, statusTextColor, accentColor) = item.IsOverdue || item.Status.Contains("overdue", StringComparison.OrdinalIgnoreCase)
            ? (Color.FromArgb("#FFF1F3"), Color.FromArgb("#F04438"), Color.FromArgb("#F04438"))
            : item.Status.Contains("due_today", StringComparison.OrdinalIgnoreCase) || item.StatusLabel.Contains("hoy", StringComparison.OrdinalIgnoreCase)
                ? (Color.FromArgb("#FFF7ED"), Color.FromArgb("#B54708"), Color.FromArgb("#F79009"))
                : (Color.FromArgb("#ECFDF3"), Color.FromArgb("#027A48"), Color.FromArgb("#12B76A"));

        return new AgendaCollectionItem(
            ClientId: item.ClientId,
            DebtId: item.DebtId,
            InstallmentId: item.InstallmentId,
            InstallmentNumber: item.InstallmentNumber,
            ClientName: item.ClientName,
            ClientInitials: BuildInitials(item.ClientName),
            AvatarColor: AvatarColorFor(item.ClientName),
            Address: item.Address,
            Description: item.Description,
            InstallmentLabel: $"Cuota {item.InstallmentNumber}",
            AmountDueText: FormatMoney(item.AmountDue),
            StatusLabel: item.StatusLabel,
            StatusBackground: statusBackground,
            StatusTextColor: statusTextColor,
            AccentColor: accentColor,
            IsOverdue: item.IsOverdue,
            IsRescheduled: item.IsRescheduled,
            RescheduleReason: item.RescheduleReason,
            DueDate: item.DueDate);
    }

    private static string BuildMonthTitle(DateTime month)
    {
        var label = month.ToString("MMMM yyyy", _mxCulture);
        return char.ToUpper(label[0], _mxCulture) + label[1..];
    }

    private static string BuildInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "CL";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
    }

    private static Color AvatarColorFor(string name)
    {
        ReadOnlySpan<string> palette = ["#2563EB", "#D97706", "#DC2626", "#0D9488",
                                        "#166534", "#7C3AED", "#DB2777", "#059669"];
        var index = Math.Abs(name.GetHashCode()) % palette.Length;
        return Color.FromArgb(palette[index]);
    }

    private static string FormatMoney(decimal amount) => $"${amount:N2}";

    private void ResetDayPanel()
    {
        DayErrorMessage = string.Empty;
        DayItems.Clear();
        SelectedDayTitle = "Selecciona un día";
        SelectedDaySubtitle = "Toca una fecha del calendario para ver los cobros programados.";
        SelectedDayAmount = "—";
        OnPropertyChanged(nameof(HasDayError));
        OnPropertyChanged(nameof(HasDayItems));
        OnPropertyChanged(nameof(HasSelectedDay));
        OnPropertyChanged(nameof(ShowDayPrompt));
        OnPropertyChanged(nameof(IsDayEmpty));
    }

    private void InvalidateMonthCache(DateTime month)
        => _monthCache.Remove(BuildMonthKey(month));

    private void InvalidateDayCacheForVisibleMonth()
    {
        var keys = _dayCache.Keys
            .Where(k => k.Year == _visibleMonth.Year && k.Month == _visibleMonth.Month)
            .ToArray();

        foreach (var key in keys)
            _dayCache.Remove(key);
    }

    private static string BuildMonthKey(DateTime month)
        => $"{month.Year:D4}-{month.Month:D2}";

    private void CancelMonthRequest(bool disposeOnly = false)
    {
        if (!disposeOnly)
            _monthCts?.Cancel();

        _monthCts?.Dispose();
        _monthCts = null;
    }

    private void CancelDayRequest(bool disposeOnly = false)
    {
        if (!disposeOnly)
            _dayCts?.Cancel();

        _dayCts?.Dispose();
        _dayCts = null;
    }

    private static async Task ShowAlertAsync(string title, string msg)
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page p)
            await p.DisplayAlertAsync(title, msg, "Entendido");
    }
}
