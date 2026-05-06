namespace Paynest.Features.Onboarding.CompleteProfile;

public partial class CompleteProfilePage : ContentPage
{
    public CompleteProfilePage(CompleteProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override bool OnBackButtonPressed()
    {
        (BindingContext as CompleteProfileViewModel)?.BackCommand.Execute(null);
        return true;
    }
}
