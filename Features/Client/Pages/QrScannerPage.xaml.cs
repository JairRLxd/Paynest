#nullable enable
using ZXing.Net.Maui;

namespace Paynest;

public partial class QrScannerPage : ContentPage
{
	private readonly Action<string> _onCodeDetected;
	private bool _hasResult;

	public QrScannerPage(Action<string> onCodeDetected)
	{
		InitializeComponent();
		_onCodeDetected = onCodeDetected;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
		if (cameraStatus != PermissionStatus.Granted)
		{
			cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
		}

		if (cameraStatus != PermissionStatus.Granted)
		{
			await DisplayAlertAsync("Permiso requerido", "Necesitamos cámara para escanear el QR.", "OK");
			await CloseAsync();
			return;
		}

		CameraView.IsDetecting = true;
	}

	private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		if (_hasResult)
		{
			return;
		}

		var rawValue = e.Results?.FirstOrDefault()?.Value;
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			return;
		}

		_hasResult = true;
		CameraView.IsDetecting = false;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			_onCodeDetected(rawValue);
			await CloseAsync();
		});
	}

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await CloseAsync();
	}

	private async Task CloseAsync()
	{
		if (Navigation.ModalStack.Contains(this))
		{
			await Navigation.PopModalAsync();
			return;
		}

		if (Navigation.NavigationStack.Contains(this))
		{
			await Navigation.PopAsync();
		}
	}
}
