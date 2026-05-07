using Paynest.Features.Cobrador.Clients.Detail.ViewModels;

namespace Paynest.Features.Cobrador.Clients.Detail.Views;

public partial class ClientDetailPage : ContentPage
{
    private readonly ClientDetailViewModel _viewModel;

    public ClientDetailPage(ClientDetailViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }
}
