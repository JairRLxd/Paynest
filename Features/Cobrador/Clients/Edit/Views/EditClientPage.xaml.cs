using Paynest.Features.Cobrador.Clients.Edit.ViewModels;

namespace Paynest.Features.Cobrador.Clients.Edit.Views;

public partial class EditClientPage : ContentPage
{
    public EditClientPage(EditClientViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
