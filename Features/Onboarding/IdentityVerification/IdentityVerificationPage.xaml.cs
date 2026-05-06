namespace Paynest.Features.Onboarding.IdentityVerification;

public partial class IdentityVerificationPage : ContentPage
{
    public IdentityVerificationPage(IdentityVerificationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
