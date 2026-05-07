namespace Paynest;

public partial class StatusBadgeView : ContentView
{
	public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
		nameof(BadgeText), typeof(string), typeof(StatusBadgeView), string.Empty);

	public static readonly BindableProperty BadgeIconProperty = BindableProperty.Create(
		nameof(BadgeIcon), typeof(string), typeof(StatusBadgeView), "•");

	public static readonly BindableProperty BadgeBackgroundProperty = BindableProperty.Create(
		nameof(BadgeBackground), typeof(Color), typeof(StatusBadgeView), Colors.Transparent);

	public static readonly BindableProperty BadgeForegroundProperty = BindableProperty.Create(
		nameof(BadgeForeground), typeof(Color), typeof(StatusBadgeView), Colors.Black);

	public StatusBadgeView()
	{
		InitializeComponent();
	}

	public string BadgeText
	{
		get => (string)GetValue(BadgeTextProperty);
		set => SetValue(BadgeTextProperty, value);
	}

	public string BadgeIcon
	{
		get => (string)GetValue(BadgeIconProperty);
		set => SetValue(BadgeIconProperty, value);
	}

	public Color BadgeBackground
	{
		get => (Color)GetValue(BadgeBackgroundProperty);
		set => SetValue(BadgeBackgroundProperty, value);
	}

	public Color BadgeForeground
	{
		get => (Color)GetValue(BadgeForegroundProperty);
		set => SetValue(BadgeForegroundProperty, value);
	}
}
