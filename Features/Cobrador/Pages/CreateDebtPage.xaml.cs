using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class CreateDebtPage : ContentPage
{
    public CreateDebtPage(CreateDebtViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
