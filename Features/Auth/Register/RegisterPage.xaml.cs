namespace Paynest.Features.Auth.Register;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(() => FirstNameEntry.Focus());
    }

    private void OnFullNameCompleted(object sender, EventArgs e) => EmailEntry.Focus();

    private void OnEmailCompleted(object sender, EventArgs e) => PasswordEntry.Focus();

    private void OnPasswordCompleted(object sender, EventArgs e) => PasswordConfirmEntry.Focus();
}
