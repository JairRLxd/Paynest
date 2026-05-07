#nullable enable
using Paynest.Features.Client.ViewModels;
using Paynest.Services;

namespace Paynest;

public partial class ReceiptsPage : ContentPage
{
	private readonly ReceiptsPageViewModel _viewModel;
	private readonly ClientRefreshController _refreshController;
	private readonly ReceiptActionService _receiptActions;

	public ReceiptsPage()
	{
		InitializeComponent();
		_viewModel = new ReceiptsPageViewModel(ServiceHelper.GetService<IClientDebtService>());
		_refreshController = new ClientRefreshController(
			ServiceHelper.GetService<ClientDataRefreshService>(),
			ClientDataRefreshScope.Receipts,
			_viewModel.RefreshAsync);
		_receiptActions = ServiceHelper.GetService<ReceiptActionService>();
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _refreshController.ActivateAsync();
		await PageMotion.FadeInUpAsync(RootLayout);
		await PageMotion.StaggerInAsync(RootLayout.Children.OfType<View>());
	}

	private async void OnDownloadReceiptClicked(object sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: string receiptId })
		{
			return;
		}

		await RunReceiptActionAsync(() => _receiptActions.DownloadReceiptAsync(receiptId));
	}

	private async void OnViewReceiptClicked(object sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: string receiptId })
		{
			return;
		}

		await RunReceiptActionAsync(() => _receiptActions.OpenReceiptAsync(receiptId));
	}

	private async void OnShareReceiptClicked(object sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: string receiptId })
		{
			return;
		}

		await RunReceiptActionAsync(() => _receiptActions.ShareReceiptAsync(receiptId));
	}

	private static async Task RunReceiptActionAsync(Func<Task<ReceiptActionResult>> action)
	{
		var result = await action();
		if (!result.Success && !string.IsNullOrWhiteSpace(result.Message))
		{
			UiFeedback.ShowShort(result.Message);
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_refreshController.Deactivate();
		_viewModel.Dispose();
	}
}
