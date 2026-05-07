#nullable enable
using Paynest.Features.Client.ViewModels;
using Paynest.Services;

namespace Paynest;

public partial class CalendarPage : ContentPage
{
	private readonly CalendarPageViewModel _viewModel;
	private readonly ClientRefreshController _refreshController;

	public CalendarPage()
	{
		InitializeComponent();
		_viewModel = new CalendarPageViewModel(ServiceHelper.GetService<IClientDebtService>());
		_refreshController = new ClientRefreshController(
			ServiceHelper.GetService<ClientDataRefreshService>(),
			ClientDataRefreshScope.Installments,
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

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_refreshController.Deactivate();
	}

	private static async void OnFilterButtonClicked(object sender, EventArgs e)
	{
		if (sender is not Button button)
		{
			return;
		}

		await button.ScaleToAsync(0.96, 70, Easing.CubicOut);
		await button.ScaleToAsync(1, 110, Easing.CubicOut);
	}
}
