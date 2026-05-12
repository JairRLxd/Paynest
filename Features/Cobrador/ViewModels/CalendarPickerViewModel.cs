using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paynest.Features.Cobrador.Models;

namespace Paynest.Features.Cobrador.ViewModels;

public partial class CalendarPickerViewModel : ObservableObject
{
    private static readonly Color SelectedBackground = Color.FromArgb("#2A6349");
    private static readonly Color SelectedText = Colors.White;
    private static readonly Color NormalText = Color.FromArgb("#1C1C1E");
    private static readonly Color MutedText = Color.FromArgb("#BAC2BC");
    private Action<DateTime>? _onDateSelected;

    [ObservableProperty] private string _title = "Selecciona una fecha";
    [ObservableProperty] private string _monthLabel = string.Empty;
    [ObservableProperty] private DateTime _visibleMonth = DateTime.Today;
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private DateTime _minimumDate = new(2020, 1, 1);

    public ObservableCollection<CalendarDayItem> Days { get; } = [];

    public void Load(string title, DateTime initialDate, DateTime minimumDate, Action<DateTime> onDateSelected)
    {
        Title = title;
        SelectedDate = initialDate.Date;
        MinimumDate = minimumDate.Date;
        VisibleMonth = new DateTime(initialDate.Year, initialDate.Month, 1);
        _onDateSelected = onDateSelected;
        BuildCalendar();
    }

    [RelayCommand]
    void PreviousMonth()
    {
        var target = VisibleMonth.AddMonths(-1);
        if (target.Date < new DateTime(MinimumDate.Year, MinimumDate.Month, 1))
            return;

        VisibleMonth = target;
        BuildCalendar();
    }

    [RelayCommand]
    void NextMonth()
    {
        VisibleMonth = VisibleMonth.AddMonths(1);
        BuildCalendar();
    }

    [RelayCommand]
    void SelectDay(CalendarDayItem? day)
    {
        if (day is null || !day.IsEnabled)
            return;

        SelectedDate = day.Date;
        BuildCalendar();
    }

    [RelayCommand]
    async Task ConfirmAsync()
    {
        _onDateSelected?.Invoke(SelectedDate);
        await CloseAsync();
    }

    [RelayCommand]
    Task CloseAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
            return page.Navigation.PopModalAsync();

        return Task.CompletedTask;
    }

    private void BuildCalendar()
    {
        Days.Clear();

        var firstDayOfMonth = new DateTime(VisibleMonth.Year, VisibleMonth.Month, 1);
        var startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstDayOfMonth.AddDays(-startOffset);

        MonthLabel = VisibleMonth.ToString("MMMM yyyy");
        MonthLabel = char.ToUpper(MonthLabel[0]) + MonthLabel[1..];

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var isCurrentMonth = date.Month == VisibleMonth.Month;
            var isEnabled = date.Date >= MinimumDate.Date;
            var isSelected = date.Date == SelectedDate.Date;

            Days.Add(new CalendarDayItem
            {
                Date = date,
                DayText = date.Day.ToString(),
                IsCurrentMonth = isCurrentMonth,
                IsEnabled = isEnabled,
                IsSelected = isSelected,
                BackgroundColor = isSelected ? SelectedBackground : Colors.Transparent,
                TextColor = isSelected
                    ? SelectedText
                    : !isCurrentMonth
                        ? MutedText
                        : isEnabled
                            ? NormalText
                            : Color.FromArgb("#D6DAD7")
            });
        }
    }
}
