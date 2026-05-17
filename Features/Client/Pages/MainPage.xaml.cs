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
		var refreshTask = _refreshController.ActivateAsync();
		await Task.WhenAll(
			refreshTask,
			PageMotion.FadeInUpAsync(RootLayout),
			PageMotion.StaggerInAsync(RootLayout.Children.OfType<View>()));
	}

	private async void OnGroupTapped(object sender, TappedEventArgs e)
	{
		if (sender is not BindableObject { BindingContext: MainPageViewModel.GroupCardItem selected })
		{
			return;
		}

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
