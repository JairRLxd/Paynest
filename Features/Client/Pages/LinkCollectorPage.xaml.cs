#nullable enable
using Paynest.Features.Client.Api;
using Paynest.Features.Client.ViewModels;
using Paynest.Services;

namespace Paynest;

public partial class LinkCollectorPage : ContentPage
{
	private readonly LinkCollectorPageViewModel _viewModel;

	public LinkCollectorPage()
	{
		InitializeComponent();
		_viewModel = new LinkCollectorPageViewModel(
			ServiceHelper.GetService<IDebtApiClient>(),
			ServiceHelper.GetService<ClientDataRefreshService>());
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await PageMotion.FadeInUpAsync(RootLayout);
	}

	private async void OnLinkClicked(object sender, EventArgs e)
	{
		var linked = await _viewModel.LinkAsync();
		if (!linked)
		{
			return;
		}

		await DisplayAlertAsync("Cobrador vinculado", "Ya puedes consultar la informacion que tu cobrador tenga registrada.", "OK");
		await Shell.Current.GoToAsync("//main");
	}

	private async void OnScanClicked(object sender, EventArgs e)
	{
		var scannerPage = new QrScannerPage(code => _viewModel.ApplyScannedOrPastedCode(code));
		await Navigation.PushModalAsync(new NavigationPage(scannerPage));
	}
}
