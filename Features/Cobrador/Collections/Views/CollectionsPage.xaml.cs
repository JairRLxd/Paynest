using Paynest.Features.Cobrador.Collections.ViewModels;

namespace Paynest.Features.Cobrador.Collections.Views;

public partial class CollectionsPage : ContentPage
{
    private readonly CollectionsViewModel _viewModel;

    public CollectionsPage(CollectionsViewModel vm)
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
