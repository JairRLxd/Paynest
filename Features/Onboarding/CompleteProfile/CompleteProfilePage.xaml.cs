namespace Paynest.Features.Onboarding.CompleteProfile;

public partial class CompleteProfilePage : ContentPage
{
    private bool _profileLoaded;

    public CompleteProfilePage(CompleteProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = (CompleteProfileViewModel)BindingContext;
        if (vm.IsEditMode && !_profileLoaded)
        {
            _profileLoaded = true;
            _ = vm.LoadProfileAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        (BindingContext as CompleteProfileViewModel)?.BackCommand.Execute(null);
        return true;
    }
}
