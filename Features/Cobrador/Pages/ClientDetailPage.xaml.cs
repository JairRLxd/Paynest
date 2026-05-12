using Paynest.Features.Cobrador;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class ClientDetailPage : ContentPage
{
    private readonly ClientDetailViewModel _viewModel;
    private CollectorRefreshController? _refreshController;

    public ClientDetailPage(ClientDetailViewModel vm, CollectorDataRefreshService refreshService)
    {
        InitializeComponent();
        _viewModel = vm;
        _refreshController = new CollectorRefreshController(
            refreshService,
            CollectorRefreshScope.ClientDetail,
            _viewModel.RefreshAsync,
            e => string.IsNullOrEmpty(e.ClientId) || e.ClientId == _viewModel.ClientId);
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _refreshController!.ActivateAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshController?.Deactivate();
    }
}
