namespace Paynest;

public partial class SectionHeaderView : ContentView
{
    public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
        nameof(TitleText), typeof(string), typeof(SectionHeaderView), string.Empty);

    public static readonly BindableProperty MetaTextProperty = BindableProperty.Create(
        nameof(MetaText), typeof(string), typeof(SectionHeaderView), string.Empty);

    public SectionHeaderView()
    {
        InitializeComponent();
    }

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string MetaText
    {
        get => (string)GetValue(MetaTextProperty);
        set => SetValue(MetaTextProperty, value);
    }
}
