using Paynest.Features.Cobrador.Clients.ViewModels;

namespace Paynest.Features.Cobrador.Clients.Views;

public partial class ClientsPage : ContentPage
{
    public ClientsPage(ClientsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
