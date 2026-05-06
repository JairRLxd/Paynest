namespace Paynest.Features.Onboarding.PaymentSetup;

public partial class PaymentSetupPage : ContentPage
{
    public PaymentSetupPage(PaymentSetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
