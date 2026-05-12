using Paynest.Features.Cobrador;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _viewModel;
    private readonly CollectorRefreshController _refreshController;

    public ClientsPage(ClientsViewModel vm, CollectorDataRefreshService refreshService)
    {
        InitializeComponent();
        _viewModel = vm;
        _refreshController = new CollectorRefreshController(
            refreshService,
            CollectorRefreshScope.Clients,
            _viewModel.RefreshAsync);
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _refreshController.ActivateAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshController.Deactivate();
    }
}
