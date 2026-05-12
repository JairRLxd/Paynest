using System.Collections.ObjectModel;

namespace Paynest.Features.Cobrador.Models;

public sealed class CollectionDebtSection : ObservableCollection<CollectionDebtItem>
{
    public CollectionDebtSection(string title, string subtitle, Color accentColor, IEnumerable<CollectionDebtItem> items)
        : base(items)
    {
        Title = title;
        Subtitle = subtitle;
        AccentColor = accentColor;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public Color AccentColor { get; }
}
