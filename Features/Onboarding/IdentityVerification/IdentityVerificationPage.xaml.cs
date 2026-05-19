namespace Paynest.Features.Onboarding.IdentityVerification;

public partial class IdentityVerificationPage : ContentPage
{
    private bool _statusLoaded;

    public IdentityVerificationPage(IdentityVerificationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = (IdentityVerificationViewModel)BindingContext;
        if (vm.IsEditMode && !_statusLoaded)
        {
            _statusLoaded = true;
            _ = vm.LoadDocumentsStatusAsync();
        }
    }
}
