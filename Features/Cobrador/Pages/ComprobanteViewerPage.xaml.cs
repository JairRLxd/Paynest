using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class ComprobanteViewerPage : ContentPage
{
    public ComprobanteViewerPage(ComprobanteViewerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
