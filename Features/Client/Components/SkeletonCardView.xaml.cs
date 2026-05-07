namespace Paynest;

public partial class SkeletonCardView : ContentView
{
    public static readonly BindableProperty ShowButtonProperty = BindableProperty.Create(
        nameof(ShowButton), typeof(bool), typeof(SkeletonCardView), false);

    public SkeletonCardView()
    {
        InitializeComponent();
    }

    public bool ShowButton
    {
        get => (bool)GetValue(ShowButtonProperty);
        set => SetValue(ShowButtonProperty, value);
    }
}
