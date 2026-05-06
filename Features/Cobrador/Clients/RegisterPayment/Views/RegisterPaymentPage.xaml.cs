using Paynest.Features.Cobrador.Clients.RegisterPayment.ViewModels;

namespace Paynest.Features.Cobrador.Clients.RegisterPayment.Views;

public partial class RegisterPaymentPage : ContentPage
{
    public RegisterPaymentPage(RegisterPaymentViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
