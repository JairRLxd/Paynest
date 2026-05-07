#nullable enable
using Paynest.Features.Client.ViewModels;
using Paynest.Features.Client.Api;
using Paynest.Services;

namespace Paynest;

public partial class CardSimulatorPage : ContentPage
{
	private readonly CardSimulatorPageViewModel _viewModel;
	private readonly ClientDataRefreshService _refreshService;
	private readonly ClientRefreshController _refreshController;

	public CardSimulatorPage()
	{
		InitializeComponent();
		_viewModel = new CardSimulatorPageViewModel(ServiceHelper.GetService<IDebtApiClient>());
		_refreshService = ServiceHelper.GetService<ClientDataRefreshService>();
		_refreshController = new ClientRefreshController(
			_refreshService,
			ClientDataRefreshScope.Wallet,
			_viewModel.LoadAsync,
			args => _viewModel.HighlightMovementAsync(args.HighlightedMovementId));
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _refreshController.ActivateAsync();
		await PageMotion.FadeInUpAsync(RootLayout);
	}

	private async void OnDepositClicked(object sender, EventArgs e)
	{
		if (!await _viewModel.DepositAsync())
		{
			if (!string.IsNullOrWhiteSpace(_viewModel.ValidationError))
			{
				UiFeedback.ShowShort(_viewModel.ValidationError);
			}
			return;
		}

		UiFeedback.ShowShort("Saldo abonado.");
		_refreshService.NotifyChanged(ClientDataRefreshScope.Wallet, _viewModel.LastCreatedMovementId);
	}

	private async void OnQuickDepositClicked(object sender, EventArgs e)
	{
		await PageScrollView.ScrollToAsync(DepositFormCard, ScrollToPosition.Start, true);
		AmountEntry.Focus();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_refreshController.Deactivate();
	}
}
