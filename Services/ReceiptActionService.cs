#nullable enable
using System.Net.Http.Headers;
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
	private readonly HttpClient _httpClient;
	private readonly AuthStateService _authState;

	public ReceiptActionService(
		IClientDebtService debtService,
		HttpClient httpClient,
		AuthStateService authState)
	{
		_debtService = debtService;
		_httpClient = httpClient;
		_authState = authState;
	}

	public async Task<ReceiptActionResult> OpenReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: false, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		var localPath = await DownloadReceiptFileAsync(receiptId, resolved.Url, cancellationToken);
		if (string.IsNullOrWhiteSpace(localPath))
		{
			return ReceiptUnavailable("No se pudo abrir el recibo por ahora.");
		}

		await Launcher.OpenAsync(new OpenFileRequest
		{
			File = new ReadOnlyFile(localPath)
		});
		return new ReceiptActionResult { Success = true, Url = resolved.Url };
	}

	public async Task<ReceiptActionResult> DownloadReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: true, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		var localPath = await DownloadReceiptFileAsync(receiptId, resolved.Url, cancellationToken);
		if (string.IsNullOrWhiteSpace(localPath))
		{
			return ReceiptUnavailable("No se pudo descargar el recibo por ahora.");
		}

		await Launcher.OpenAsync(new OpenFileRequest
		{
			File = new ReadOnlyFile(localPath)
		});
		return new ReceiptActionResult { Success = true, Url = resolved.Url };
	}

	public async Task<ReceiptActionResult> ShareReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
	{
		var resolved = await ResolveReceiptUrlAsync(receiptId, preferDownloadEndpoint: false, cancellationToken);
		if (!resolved.Success)
		{
			return resolved;
		}

		var localPath = await DownloadReceiptFileAsync(receiptId, resolved.Url, cancellationToken);
		if (string.IsNullOrWhiteSpace(localPath))
		{
			return ReceiptUnavailable("No se pudo preparar el recibo para compartir.");
		}

		await Share.Default.RequestAsync(new ShareFileRequest
		{
			File = new ShareFile(localPath),
			Title = "Compartir recibo"
		});
		return new ReceiptActionResult { Success = true, Url = resolved.Url };
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

	private async Task<string?> DownloadReceiptFileAsync(string receiptId, string remoteUrl, CancellationToken cancellationToken)
	{
		if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
		{
			return null;
		}

		return await _authState.CallProtectedAsync(async token =>
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, uri);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			res.EnsureSuccessStatusCode();

			var bytes = await res.Content.ReadAsByteArrayAsync(cancellationToken);
			if (bytes.Length == 0)
			{
				return null;
			}

			var folder = Path.Combine(FileSystem.CacheDirectory, "receipts");
			Directory.CreateDirectory(folder);
			var filePath = Path.Combine(folder, BuildReceiptFileName(receiptId));
			await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
			return filePath;
		}, cancellationToken);
	}

	private static string BuildReceiptFileName(string receiptId)
	{
		var safe = string.Join("_", receiptId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
		return $"{(string.IsNullOrWhiteSpace(safe) ? "recibo" : safe)}.pdf";
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
