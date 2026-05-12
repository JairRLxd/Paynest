using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class ClientPickerPage : ContentPage
{
    public ClientPickerPage(ClientPickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
