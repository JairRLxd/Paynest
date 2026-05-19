namespace Paynest.Features.Auth.ForgotPassword;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ForgotPasswordViewModel _vm;

    public ForgotPasswordPage(ForgotPasswordViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.PropertyChanged -= OnVmPropertyChanged;
    }

    private async void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForgotPasswordViewModel.HasSuccess) && _vm.HasSuccess)
        {
            await Task.Delay(1800);
            await GoToLoginAsync();
        }
    }

    private async void OnGoToLoginTapped(object? sender, TappedEventArgs e)
        => await GoToLoginAsync();

    private async Task GoToLoginAsync()
    {
        var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation is not null)
            await navigation.PopAsync();
    }
}
