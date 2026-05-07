using Paynest.Features.Cobrador.Clients.AddClient.ViewModels;

namespace Paynest.Features.Cobrador.Clients.AddClient.Views;

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
