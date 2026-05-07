#nullable enable
using Paynest.Features.Client.ViewModels;

namespace Paynest;

public partial class PageStateContainer : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State),
        typeof(ScreenState),
        typeof(PageStateContainer),
        ScreenState.Loading,
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty LoadingContentProperty = BindableProperty.Create(
        nameof(LoadingContent),
        typeof(View),
        typeof(PageStateContainer),
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty ContentBodyProperty = BindableProperty.Create(
        nameof(ContentBody),
        typeof(View),
        typeof(PageStateContainer),
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty EmptyContentProperty = BindableProperty.Create(
        nameof(EmptyContent),
        typeof(View),
        typeof(PageStateContainer),
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty ErrorContentProperty = BindableProperty.Create(
        nameof(ErrorContent),
        typeof(View),
        typeof(PageStateContainer),
        propertyChanged: OnBindablePropertyChanged);

    public PageStateContainer()
    {
        InitializeComponent();
        ApplyContent();
        ApplyState();
    }

    public ScreenState State
    {
        get => (ScreenState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public View? LoadingContent
    {
        get => (View?)GetValue(LoadingContentProperty);
        set => SetValue(LoadingContentProperty, value);
    }

    public View? ContentBody
    {
        get => (View?)GetValue(ContentBodyProperty);
        set => SetValue(ContentBodyProperty, value);
    }

    public View? EmptyContent
    {
        get => (View?)GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public View? ErrorContent
    {
        get => (View?)GetValue(ErrorContentProperty);
        set => SetValue(ErrorContentProperty, value);
    }

    private void ApplyContent()
    {
        LoadingHost.Content = LoadingContent;
        ContentHost.Content = ContentBody;
        EmptyHost.Content = EmptyContent;
        ErrorHost.Content = ErrorContent;
    }

    private void ApplyState()
    {
        LoadingHost.IsVisible = State == ScreenState.Loading;
        ContentHost.IsVisible = State == ScreenState.Content;
        EmptyHost.IsVisible = State == ScreenState.Empty;
        ErrorHost.IsVisible = State == ScreenState.Error;
    }

    private static void OnBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (PageStateContainer)bindable;
        view.ApplyContent();
        view.ApplyState();
    }
}
