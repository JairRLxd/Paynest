namespace Paynest.Features.Cobrador.Clients.Detail.Views;

public partial class PhotoViewerPage : ContentPage
{
    public PhotoViewerPage(ImageSource source)
    {
        InitializeComponent();
        photoImage.Source = source;
    }

    private async void OnCloseAsync(object? sender, TappedEventArgs e)
        => await Navigation.PopModalAsync();
}
