using Paynest.Features.Cobrador.Clients.AddClient.ViewModels;

namespace Paynest.Features.Cobrador.Clients.AddClient.Views;

public partial class AddClientPage : ContentPage
{
    public AddClientPage(AddClientViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
