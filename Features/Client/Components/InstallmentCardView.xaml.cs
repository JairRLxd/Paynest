namespace Paynest;

public partial class InstallmentCardView : ContentView
{
    public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
        nameof(TitleText), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty AmountTextProperty = BindableProperty.Create(
        nameof(AmountText), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
        nameof(DetailText), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty StatusIconProperty = BindableProperty.Create(
        nameof(StatusIcon), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
        nameof(StatusText), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty StatusBackgroundProperty = BindableProperty.Create(
        nameof(StatusBackground), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty StatusForegroundProperty = BindableProperty.Create(
        nameof(StatusForeground), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty ShowActionButtonProperty = BindableProperty.Create(
        nameof(ShowActionButton), typeof(bool), typeof(InstallmentCardView), false);

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(InstallmentCardView), false);

    public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
        nameof(IsActionEnabled), typeof(bool), typeof(InstallmentCardView), true);

    public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
        nameof(ActionSemanticDescription), typeof(string), typeof(InstallmentCardView), string.Empty);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(InstallmentCardView));

    public event EventHandler? Clicked;

    public InstallmentCardView()
    {
        InitializeComponent();
    }

    public string TitleText { get => (string)GetValue(TitleTextProperty); set => SetValue(TitleTextProperty, value); }
    public string AmountText { get => (string)GetValue(AmountTextProperty); set => SetValue(AmountTextProperty, value); }
    public string DetailText { get => (string)GetValue(DetailTextProperty); set => SetValue(DetailTextProperty, value); }
    public string StatusIcon { get => (string)GetValue(StatusIconProperty); set => SetValue(StatusIconProperty, value); }
    public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    public string StatusBackground { get => (string)GetValue(StatusBackgroundProperty); set => SetValue(StatusBackgroundProperty, value); }
    public string StatusForeground { get => (string)GetValue(StatusForegroundProperty); set => SetValue(StatusForegroundProperty, value); }
    public bool ShowActionButton { get => (bool)GetValue(ShowActionButtonProperty); set => SetValue(ShowActionButtonProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }
    public bool IsActionEnabled { get => (bool)GetValue(IsActionEnabledProperty); set => SetValue(IsActionEnabledProperty, value); }
    public string ActionSemanticDescription { get => (string)GetValue(ActionSemanticDescriptionProperty); set => SetValue(ActionSemanticDescriptionProperty, value); }
    public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private void OnActionClicked(object sender, EventArgs e)
    {
        Clicked?.Invoke(sender, e);
    }
}
