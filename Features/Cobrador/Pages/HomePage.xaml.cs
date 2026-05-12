using Paynest.Features.Cobrador;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly CollectorRefreshController _refreshController;

    public HomePage(HomeViewModel vm, CollectorDataRefreshService refreshService)
    {
        InitializeComponent();
        _viewModel = vm;
        _refreshController = new CollectorRefreshController(
            refreshService,
            CollectorRefreshScope.Dashboard,
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
