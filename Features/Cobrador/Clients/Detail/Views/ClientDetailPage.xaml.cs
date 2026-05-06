using Paynest.Features.Cobrador.Clients.Detail.ViewModels;

namespace Paynest.Features.Cobrador.Clients.Detail.Views;

public partial class ClientDetailPage : ContentPage
{
    public ClientDetailPage(ClientDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
