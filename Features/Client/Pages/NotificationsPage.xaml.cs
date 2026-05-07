using Paynest.Features.Client.ViewModels;
using Paynest.Services;

namespace Paynest;

public partial class NotificationsPage : ContentPage
{
	private readonly NotificationsPageViewModel _viewModel;

	public NotificationsPage()
	{
		InitializeComponent();
		_viewModel = new NotificationsPageViewModel(ServiceHelper.GetService<IClientDebtService>());
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			await _viewModel.LoadAsync();
		}
		catch
		{
			UiFeedback.ShowShort("No fue posible cargar preferencias.");
		}
		await PageMotion.FadeInUpAsync(RootLayout);
	}

	private async void OnSaveClicked(object sender, EventArgs e)
	{
		var summary = await _viewModel.SaveAsync();
		if (string.IsNullOrWhiteSpace(summary))
		{
			return;
		}
		UiFeedback.ShowShort("Preferencias guardadas.");
	}
}
