using Paynest.Features.Cobrador.Clients.ComprobanteViewer.ViewModels;

namespace Paynest.Features.Cobrador.Clients.ComprobanteViewer.Views;

public partial class ComprobanteViewerPage : ContentPage
{
    public ComprobanteViewerPage(ComprobanteViewerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
