#nullable enable
using Paynest.Core.Validation;
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
	}

	public Command LinkCommand { get; }
	public Command PasteCommand { get; }

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

		var normalizedCode = ExtractCollectorCode(CollectorCode);
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
			ApplyScannedOrPastedCode(text);
		}
	}

	public void ApplyScannedOrPastedCode(string rawValue)
	{
		CollectorCode = ExtractCollectorCode(rawValue);
	}

	private static string ExtractCollectorCode(string value)
	{
		var trimmed = value.Trim();
		if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
		{
			var fromQuery = uri.Query.TrimStart('?')
				.Split('&', StringSplitOptions.RemoveEmptyEntries)
				.Select(part => part.Split('=', 2))
				.FirstOrDefault(parts =>
					parts.Length == 2 &&
					string.Equals(parts[0], "collectorCode", StringComparison.OrdinalIgnoreCase));
			if (fromQuery is { Length: 2 })
			{
				trimmed = Uri.UnescapeDataString(fromQuery[1]);
			}
			else
			{
				trimmed = uri.Segments.LastOrDefault()?.Trim('/') ?? trimmed;
			}
		}

		var code = InputSanitizer.Identifier(trimmed);
		if (string.IsNullOrWhiteSpace(code))
		{
			return string.Empty;
		}

		return code.StartsWith("PAY-", StringComparison.Ordinal) ? code : $"PAY-{code}";
	}
}
