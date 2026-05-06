using Paynest.Features.Cobrador.Home.ViewModels;

namespace Paynest.Features.Cobrador.Home.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
