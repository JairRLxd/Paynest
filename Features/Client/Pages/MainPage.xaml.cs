#nullable enable
using Paynest.Features.Client.ViewModels;
using Paynest.Features.Client.Api;
using Paynest.Services;

namespace Paynest;

public partial class MainPage : ContentPage
{
	private readonly MainPageViewModel _viewModel;
	private readonly ClientRefreshController _refreshController;

	public MainPage()
	{
		InitializeComponent();
		_viewModel = new MainPageViewModel(
			ServiceHelper.GetService<IClientDebtService>(),
			ServiceHelper.GetService<IDebtApiClient>());
		_refreshController = new ClientRefreshController(
			ServiceHelper.GetService<ClientDataRefreshService>(),
			ClientDataRefreshScope.Debts | ClientDataRefreshScope.Installments | ClientDataRefreshScope.Wallet,
			_viewModel.RefreshAsync);
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _refreshController.ActivateAsync();
		await PageMotion.FadeInUpAsync(RootLayout);
		await PageMotion.StaggerInAsync(RootLayout.Children.OfType<View>());
	}

	private async void OnGroupSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not MainPageViewModel.GroupCardItem selected)
		{
			return;
		}

		((CollectionView)sender!).SelectedItem = null;
		await Shell.Current.GoToAsync($"{nameof(DebtDetailPage)}?groupId={Uri.EscapeDataString(selected.Id)}");
	}

	private async void OnWalletSummaryClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//card");
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_refreshController.Deactivate();
		_viewModel.Dispose();
	}
}
