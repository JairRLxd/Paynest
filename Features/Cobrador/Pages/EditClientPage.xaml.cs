using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class EditClientPage : ContentPage
{
    public EditClientPage(EditClientViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
