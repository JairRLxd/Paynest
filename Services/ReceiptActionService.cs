#nullable enable
using Paynest.Models;

namespace Paynest.Services;

public sealed class ReceiptActionResult
{
	public bool Success { get; init; }
	public string Message { get; init; } = string.Empty;
	public string Url { get; init; } = string.Empty;
}

public sealed class ReceiptActionService
{
	private readonly IClientDebtService _debtService;

	public ReceiptActionService(IClientDebtService debtService)
	{
		_debtService = debtService;
	}

	public async Task<ReceiptActionResult> OpenReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: false, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		await Launcher.OpenAsync(new Uri(resolved.Url));
		return resolved;
	}

	public async Task<ReceiptActionResult> DownloadReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: true, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		await Launcher.OpenAsync(new Uri(resolved.Url));
		return resolved;
	}

	public async Task<ReceiptActionResult> ShareReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: false, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		await Share.Default.RequestAsync(new ShareTextRequest
		{
			Uri = resolved.Url,
			Title = "Compartir recibo"
		});
		return resolved;
	}

	public async Task<ReceiptActionResult> OpenPaymentReceiptAsync(PayInstallmentResult? paymentResult, CancellationToken cancellationToken = default)
	{
		if (paymentResult is null || string.IsNullOrWhiteSpace(paymentResult.ReceiptId))
		{
			return ReceiptUnavailable("El recibo se registró, pero todavía no está listo para abrirse.");
		}

		if (Uri.TryCreate(paymentResult.ReceiptFileUrl, UriKind.Absolute, out var fileUri))
		{
			await Launcher.OpenAsync(fileUri);
			return new ReceiptActionResult { Success = true, Url = fileUri.ToString() };
		}

		return await OpenReceiptAsync(paymentResult.ReceiptId, cancellationToken);
	}

	private async Task<ReceiptActionResult> ResolveReceiptUrlAsync(
		string receiptId,
		bool preferDownloadEndpoint,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(receiptId))
		{
			return ReceiptUnavailable("No encontramos el recibo solicitado.");
		}

		try
		{
			var url = preferDownloadEndpoint
				? await _debtService.GetReceiptDownloadUrlAsync(receiptId, cancellationToken)
				: await ResolveViewUrlAsync(receiptId, cancellationToken);

			return Uri.TryCreate(url, UriKind.Absolute, out var uri)
				? new ReceiptActionResult { Success = true, Url = uri.ToString() }
				: ReceiptUnavailable("El recibo existe, pero aún no tiene archivo disponible.");
		}
		catch (OperationCanceledException)
		{
			return ReceiptUnavailable(string.Empty);
		}
		catch (Exception ex)
		{
			return ReceiptUnavailable(NetworkErrorMessageProvider.From(ex));
		}
	}

	private async Task<string?> ResolveViewUrlAsync(string receiptId, CancellationToken cancellationToken)
	{
		var receipt = await _debtService.GetReceiptAsync(receiptId, cancellationToken);
		if (receipt is null)
		{
			return null;
		}

		return string.IsNullOrWhiteSpace(receipt.FileUrl)
			? await _debtService.GetReceiptDownloadUrlAsync(receiptId, cancellationToken)
			: receipt.FileUrl;
	}

	private static ReceiptActionResult ReceiptUnavailable(string message)
	{
		return new ReceiptActionResult
		{
			Success = false,
			Message = string.IsNullOrWhiteSpace(message)
				? "El recibo no está disponible todavía."
				: message
		};
	}
}
