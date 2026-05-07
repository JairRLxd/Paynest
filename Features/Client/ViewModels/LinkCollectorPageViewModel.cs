#nullable enable
using Paynest.Features.Client.Api;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class LinkCollectorPageViewModel : BaseViewModel
{
	private readonly IDebtApiClient _apiClient;
	private readonly ClientDataRefreshService _refreshService;
	private string _collectorCode = string.Empty;
	private string _errorMessage = string.Empty;
	private string _successMessage = string.Empty;
	private bool _isBusy;

	public LinkCollectorPageViewModel(IDebtApiClient apiClient, ClientDataRefreshService refreshService)
	{
		_apiClient = apiClient;
		_refreshService = refreshService;
		LinkCommand = new Command(async () => await LinkAsync(), () => IsNotBusy);
		PasteCommand = new Command(async () => await PasteAsync());
		ScanCommand = new Command(async () => await ShowScanPlaceholderAsync());
	}

	public Command LinkCommand { get; }
	public Command PasteCommand { get; }
	public Command ScanCommand { get; }

	public string CollectorCode
	{
		get => _collectorCode;
		set
		{
			SetProperty(ref _collectorCode, value);
			ErrorMessage = string.Empty;
			SuccessMessage = string.Empty;
		}
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		private set
		{
			SetProperty(ref _errorMessage, value);
			RaisePropertyChanged(nameof(HasError));
		}
	}

	public string SuccessMessage
	{
		get => _successMessage;
		private set
		{
			SetProperty(ref _successMessage, value);
			RaisePropertyChanged(nameof(HasSuccess));
		}
	}

	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			SetProperty(ref _isBusy, value);
			RaisePropertyChanged(nameof(IsNotBusy));
			RaisePropertyChanged(nameof(LinkButtonText));
			LinkCommand.ChangeCanExecute();
		}
	}

	public bool IsNotBusy => !IsBusy;
	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
	public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
	public string LinkButtonText => IsBusy ? "Vinculando..." : "Vincular cobrador";

	public async Task<bool> LinkAsync(CancellationToken cancellationToken = default)
	{
		if (IsBusy)
		{
			return false;
		}

		var normalizedCode = NormalizeCollectorCode(CollectorCode);
		if (string.IsNullOrWhiteSpace(normalizedCode))
		{
			ErrorMessage = "Ingresa el codigo que te compartio tu cobrador.";
			return false;
		}

		try
		{
			IsBusy = true;
			ErrorMessage = string.Empty;
			SuccessMessage = string.Empty;
			var response = await _apiClient.LinkCollectorAsync(
				new LinkCollectorRequestDto { CollectorCode = normalizedCode },
				cancellationToken);

			CollectorCode = normalizedCode;
			SuccessMessage = string.IsNullOrWhiteSpace(response.CollectorName)
				? "Cobrador vinculado correctamente."
				: $"Te vinculaste con {response.CollectorName}.";
			_refreshService.NotifyChanged(ClientDataRefreshScope.All);
			return true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch (Exception ex)
		{
			ErrorMessage = NetworkErrorMessageProvider.From(ex);
			return false;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task PasteAsync()
	{
		var text = await Clipboard.Default.GetTextAsync();
		if (!string.IsNullOrWhiteSpace(text))
		{
			CollectorCode = NormalizeCollectorCode(text);
		}
	}

	private static async Task ShowScanPlaceholderAsync()
	{
		if (Shell.Current is not null)
		{
			await Shell.Current.DisplayAlertAsync(
				"Escanear QR",
				"El escaneo queda preparado para cuando agreguemos el lector de QR. Por ahora pega o escribe el codigo.",
				"OK");
		}
	}

	private static string NormalizeCollectorCode(string value)
	{
		var code = value.Trim().ToUpperInvariant().Replace(" ", string.Empty);
		if (string.IsNullOrWhiteSpace(code))
		{
			return string.Empty;
		}

		return code.StartsWith("PAY-", StringComparison.Ordinal) ? code : $"PAY-{code}";
	}
}
