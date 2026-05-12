using CommunityToolkit.Mvvm.ComponentModel;

namespace Paynest.Features.Cobrador.Models;

public partial class CalendarDayItem : ObservableObject
{
    public DateTime Date { get; init; }
    public string DayText { get; init; } = string.Empty;

    [ObservableProperty] private bool _isCurrentMonth;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Color _backgroundColor = Colors.Transparent;
    [ObservableProperty] private Color _textColor = Color.FromArgb("#1C1C1E");
}
