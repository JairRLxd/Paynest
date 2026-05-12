using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class RegisterPaymentPage : ContentPage
{
    public RegisterPaymentPage(RegisterPaymentViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
