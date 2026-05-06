using Paynest.Features.Cobrador.Clients.ClientPicker.ViewModels;

namespace Paynest.Features.Cobrador.Clients.ClientPicker.Views;

public partial class ClientPickerPage : ContentPage
{
    public ClientPickerPage(ClientPickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
