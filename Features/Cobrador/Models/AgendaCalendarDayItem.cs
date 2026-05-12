using CommunityToolkit.Mvvm.ComponentModel;

namespace Paynest.Features.Cobrador.Models;

public partial class AgendaCalendarDayItem : ObservableObject
{
    public DateTime Date { get; init; }
    public string DayText { get; init; } = string.Empty;
    public string ItemsCountText { get; init; } = string.Empty;

    [ObservableProperty] private bool _isCurrentMonth;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isToday;
    [ObservableProperty] private bool _hasItems;
    [ObservableProperty] private bool _hasOverdue;
    [ObservableProperty] private bool _hasRescheduled;
    [ObservableProperty] private Color _backgroundColor = Colors.Transparent;
    [ObservableProperty] private Color _borderColor = Color.FromArgb("#E3E9E4");
    [ObservableProperty] private Color _dayTextColor = Color.FromArgb("#1F2937");
    [ObservableProperty] private Color _itemsBadgeBackground = Color.FromArgb("#EEF4F0");
    [ObservableProperty] private Color _itemsBadgeTextColor = Color.FromArgb("#2A6349");
    [ObservableProperty] private double _opacity = 1;
}
