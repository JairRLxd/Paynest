namespace Paynest.Features.Onboarding.PaymentSetup;

public partial class PaymentSetupPage : ContentPage
{
    private bool _configLoaded;

    public PaymentSetupPage(PaymentSetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = (PaymentSetupViewModel)BindingContext;
        if (vm.IsEditMode && !_configLoaded)
        {
            _configLoaded = true;
            _ = vm.LoadPaymentConfigAsync();
        }
    }
}
