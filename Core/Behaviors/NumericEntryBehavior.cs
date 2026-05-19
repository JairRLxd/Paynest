namespace Paynest.Core.Behaviors;

/// <summary>
/// Strips any character that is not a digit or decimal separator so that
/// numeric Entry controls never receive letter input regardless of platform keyboard.
/// </summary>
public class NumericEntryBehavior : Behavior<Entry>
{
    public static readonly BindableProperty AllowDecimalProperty =
        BindableProperty.Create(nameof(AllowDecimal), typeof(bool), typeof(NumericEntryBehavior), true);

    public bool AllowDecimal
    {
        get => (bool)GetValue(AllowDecimalProperty);
        set => SetValue(AllowDecimalProperty, value);
    }

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.TextChanged += OnTextChanged;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || e.NewTextValue is null)
            return;

        var filtered = Filter(e.NewTextValue);
        if (filtered != e.NewTextValue)
            entry.Text = filtered;
    }

    private string Filter(string raw)
    {
        bool dotSeen = false;
        var chars = new System.Text.StringBuilder(raw.Length);

        foreach (char c in raw)
        {
            if (char.IsDigit(c))
            {
                chars.Append(c);
            }
            else if (AllowDecimal && (c == '.' || c == ',') && !dotSeen)
            {
                dotSeen = true;
                chars.Append('.');
            }
        }

        return chars.ToString();
    }
}
