using Paynest.Features.Cobrador.ViewModels;

namespace Paynest.Features.Cobrador.Pages;

public partial class AddClientPage : ContentPage
{
    private readonly AddClientViewModel _viewModel;

    public AddClientPage(AddClientViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInviteAsync();
    }
}
