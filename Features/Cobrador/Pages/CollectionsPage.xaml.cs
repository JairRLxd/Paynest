using Paynest.Features.Cobrador;
using Paynest.Features.Cobrador.ViewModels;
using Paynest.Services;

namespace Paynest.Features.Cobrador.Pages;

public partial class CollectionsPage : ContentPage
{
    private readonly CollectionsViewModel _viewModel;
    private readonly CollectorRefreshController _refreshController;

    public CollectionsPage(CollectionsViewModel vm, CollectorDataRefreshService refreshService)
    {
        InitializeComponent();
        _viewModel = vm;
        _refreshController = new CollectorRefreshController(
            refreshService,
            CollectorRefreshScope.Collections,
            _viewModel.RefreshAsync);
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _refreshController.ActivateAsync();

        if (CollectionsPendingFilter.Consume() is { } filter)
            await _viewModel.ApplyFilterAsync(filter);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshController.Deactivate();
    }
}
