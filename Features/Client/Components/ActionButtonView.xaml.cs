#nullable enable
using System.Windows.Input;

namespace Paynest;

public partial class ActionButtonView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ActionButtonView), string.Empty);

    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(ActionButtonView), false);

    public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
        nameof(IsActionEnabled), typeof(bool), typeof(ActionButtonView), true);

    public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
        nameof(SemanticDescription), typeof(string), typeof(ActionButtonView), string.Empty);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(ActionButtonView));

    public event EventHandler? Clicked;

    public ActionButtonView()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public bool IsActionEnabled
    {
        get => (bool)GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    public string SemanticDescription
    {
        get => (string)GetValue(SemanticDescriptionProperty);
        set => SetValue(SemanticDescriptionProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private void OnActionButtonClicked(object sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
    }
}
