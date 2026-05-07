namespace Paynest;

public partial class NetworkBannerView : ContentView
{
    public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
        nameof(TitleText), typeof(string), typeof(NetworkBannerView), "Sin conexión");

    public static readonly BindableProperty MessageTextProperty = BindableProperty.Create(
        nameof(MessageText), typeof(string), typeof(NetworkBannerView), "Intenta nuevamente.");

    public NetworkBannerView()
    {
        InitializeComponent();
    }

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string MessageText
    {
        get => (string)GetValue(MessageTextProperty);
        set => SetValue(MessageTextProperty, value);
    }
}
