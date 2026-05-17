#nullable enable
using Paynest.Features.Client.Api;
using Paynest.Features.Client.ViewModels;
using Paynest.Services;

namespace Paynest;

public partial class DebtDetailPage : ContentPage, IQueryAttributable
{
	private readonly DebtDetailPageViewModel _viewModel;
	private readonly IDebtApiClient _debtApiClient;
	private readonly ClientDataRefreshService _refreshService;
	private readonly ReceiptActionService _receiptActions;
	private string _lastPaidId = string.Empty;
	private CancellationTokenSource? _requestCts;
	private TaskCompletionSource<bool>? _paymentSheetCompletion;
	private TaskCompletionSource<string>? _paymentSuccessCompletion;

	public DebtDetailPage()
	{
		InitializeComponent();
		_viewModel = new DebtDetailPageViewModel(ServiceHelper.GetService<IClientDebtService>());
		_debtApiClient = ServiceHelper.GetService<IDebtApiClient>();
		_refreshService = ServiceHelper.GetService<ClientDataRefreshService>();
		_receiptActions = ServiceHelper.GetService<ReceiptActionService>();
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_requestCts?.Cancel();
		_requestCts = new CancellationTokenSource();
		await PageMotion.FadeInUpAsync(RootLayout);
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("groupId", out var groupIdValue) && groupIdValue is string groupId)
		{
			_ = LoadGroupSafeAsync(groupId);
		}
	}

	private async void OnPayInstallmentClicked(object sender, EventArgs e)
	{
		if (sender is not ActionButtonView { CommandParameter: string installmentId } actionButton)
		{
			return;
		}

		var row = actionButton.BindingContext as DebtDetailPageViewModel.InstallmentRowItem;
		if (!await ConfirmPaymentAsync(row, _requestCts?.Token ?? CancellationToken.None))
		{
			return;
		}

		try
		{
			var paid = await _viewModel.PayInstallmentAsync(installmentId, _requestCts?.Token ?? CancellationToken.None);
			if (!paid)
			{
				UiFeedback.ShowShort("No pudimos aplicar el pago. Actualiza e intenta de nuevo.");
				return;
			}
		}
		catch (HttpRequestException ex) when (ex.Message.Contains("Saldo insuficiente", StringComparison.OrdinalIgnoreCase))
		{
			var insufficientAction = await DisplayActionSheetAsync(
				"No tienes saldo suficiente para pagar esta cuota.",
				"Seguir aquí",
				null,
				"Abonar saldo");
			if (insufficientAction == "Abonar saldo")
			{
				await Shell.Current.GoToAsync("//card");
			}
			return;
		}
		catch (HttpRequestException ex) when (ContainsAny(ex, "Cuota ya pagada", "already paid"))
		{
			await _viewModel.ReloadAsync(_requestCts?.Token ?? CancellationToken.None);
			UiFeedback.ShowShort("Esta cuota ya estaba pagada. Actualicé el detalle.");
			return;
		}
		catch (HttpRequestException ex) when (ContainsAny(ex, "Saldo no disponible", "wallet inactiva", "wallet inactive"))
		{
			await DisplayAlertAsync("Saldo no disponible", "Tu saldo Paynest no está disponible para procesar este pago.", "Entendido");
			return;
		}
		catch (HttpRequestException ex) when (ContainsAny(ex, "Cuota no encontrada", "not found"))
		{
			var missingAction = await DisplayActionSheetAsync(
				"No encontramos esta cuota o ya no pertenece a tu cuenta.",
				"Seguir aquí",
				null,
				"Actualizar",
				"Volver");
			if (missingAction == "Actualizar")
			{
				await _viewModel.ReloadAsync(_requestCts?.Token ?? CancellationToken.None);
			}
			else if (missingAction == "Volver")
			{
				await Shell.Current.GoToAsync("..");
			}
			return;
		}
		catch (Exception ex)
		{
			UiFeedback.ShowShort(NetworkErrorMessageProvider.From(ex));
			return;
		}

		if (string.IsNullOrWhiteSpace(_viewModel.LastPaidInstallmentId) || _viewModel.LastPaidInstallmentId == _lastPaidId)
		{
			return;
		}

		_lastPaidId = _viewModel.LastPaidInstallmentId;
		_refreshService.NotifyChanged(
			ClientDataRefreshScope.Debts | ClientDataRefreshScope.Installments | ClientDataRefreshScope.Receipts | ClientDataRefreshScope.Wallet,
			_viewModel.LastPaymentResult?.MovementId ?? string.Empty);
		var nextAction = await ShowPaymentSuccessSheetAsync(_viewModel.LastPaymentBalanceText);
		if (nextAction == "Ver recibo")
		{
			var receiptResult = await _receiptActions.OpenPaymentReceiptAsync(_viewModel.LastPaymentResult, _requestCts?.Token ?? CancellationToken.None);
			if (!receiptResult.Success && !string.IsNullOrWhiteSpace(receiptResult.Message))
			{
				UiFeedback.ShowShort(receiptResult.Message);
			}
		}
		else if (nextAction == "Ver recibos")
		{
			await Shell.Current.GoToAsync("//receipts");
		}
	}

	private async Task<string> ShowPaymentSuccessSheetAsync(string balanceText)
	{
		_paymentSuccessCompletion?.TrySetResult("Seguir aquí");
		_paymentSuccessCompletion = new TaskCompletionSource<string>();

		var method = _viewModel.LastPaymentMethodText;
		PaymentSuccessSubtitleLabel.Text = _viewModel.WasDebtSettledByLastPayment
			? $"Pagado con {method} · Liquidaste esta deuda y el recibo final está listo."
			: $"Pagado con {method} · La cuota quedó registrada y el recibo está listo.";
		PaymentSuccessBalanceLabel.Text = string.IsNullOrWhiteSpace(balanceText) ? "Actualizado" : balanceText;

		PaymentSuccessOverlay.Opacity = 0;
		PaymentSuccessPanel.TranslationY = 48;
		PaymentSuccessOverlay.IsVisible = true;
		await Task.WhenAll(
			PaymentSuccessOverlay.FadeToAsync(1, 150),
			PaymentSuccessPanel.TranslateToAsync(0, 0, 220, Easing.CubicOut));

		return await _paymentSuccessCompletion.Task;
	}

	private async Task ClosePaymentSuccessSheetAsync(string action)
	{
		var completion = _paymentSuccessCompletion;
		if (completion is null)
		{
			return;
		}

		_paymentSuccessCompletion = null;
		await Task.WhenAll(
			PaymentSuccessPanel.TranslateToAsync(0, 48, 180, Easing.CubicIn),
			PaymentSuccessOverlay.FadeToAsync(0, 160));
		PaymentSuccessOverlay.IsVisible = false;
		completion.TrySetResult(action);
	}

	private async void OnPaymentSuccessViewReceiptClicked(object sender, EventArgs e)
	{
		await ClosePaymentSuccessSheetAsync("Ver recibo");
	}

	private async void OnPaymentSuccessViewReceiptsClicked(object sender, EventArgs e)
	{
		await ClosePaymentSuccessSheetAsync("Ver recibos");
	}

	private async void OnPaymentSuccessContinueClicked(object sender, EventArgs e)
	{
		await ClosePaymentSuccessSheetAsync("Seguir aquí");
	}

	private async void OnViewReceiptsFromSettledStateClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//receipts");
	}

	private async Task<bool> ConfirmPaymentAsync(DebtDetailPageViewModel.InstallmentRowItem? row, CancellationToken cancellationToken)
	{
		if (row is null)
		{
			return await ShowPaymentSheetAsync(
				title: "Cuota seleccionada",
				amountText: "Por confirmar",
				availableBalanceText: "Por confirmar",
				afterTitleText: "Saldo después del pago",
				afterText: "Por confirmar",
				statusText: "Pago con Saldo Paynest",
				messageText: "Esta cuota se moverá a Pagadas y se preparará su recibo.",
				canPay: true);
		}

		try
		{
			var wallet = await _debtApiClient.GetWalletAsync(cancellationToken);
			var remainingBalance = wallet.Balance - row.Amount;
			if (remainingBalance < 0)
			{
				var missingAmount = Math.Abs(remainingBalance);
				var shouldDeposit = await ShowPaymentSheetAsync(
					title: row.Title,
					amountText: row.AmountText,
					availableBalanceText: wallet.Balance.ToString("C"),
					afterTitleText: "Monto faltante",
					afterText: missingAmount.ToString("C"),
					statusText: "Saldo insuficiente",
					messageText: "Abona saldo para completar este pago.",
					canPay: false);
				if (shouldDeposit)
				{
					await Shell.Current.GoToAsync("//card");
				}

				return false;
			}

			return await ShowPaymentSheetAsync(
				title: row.Title,
				amountText: row.AmountText,
				availableBalanceText: wallet.Balance.ToString("C"),
				afterTitleText: "Saldo después del pago",
				afterText: remainingBalance.ToString("C"),
				statusText: "Pago con Saldo Paynest",
				messageText: "Revisa el monto antes de confirmar. El recibo se generará al completarse el pago.",
				canPay: true);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			UiFeedback.ShowShort("No pude consultar tu saldo. Intentemos de nuevo.");
			return false;
		}
	}

	private async Task<bool> ShowPaymentSheetAsync(
		string title,
		string amountText,
		string availableBalanceText,
		string afterTitleText,
		string afterText,
		string statusText,
		string messageText,
		bool canPay)
	{
		_paymentSheetCompletion?.TrySetResult(false);
		_paymentSheetCompletion = new TaskCompletionSource<bool>();

		PaymentSheetTitleLabel.Text = title;
		PaymentSheetAmountLabel.Text = amountText;
		PaymentSheetBalanceLabel.Text = availableBalanceText;
		PaymentSheetAfterTitleLabel.Text = afterTitleText;
		PaymentSheetAfterLabel.Text = afterText;
		PaymentSheetStatusLabel.Text = statusText;
		PaymentSheetMessageLabel.Text = messageText;
		PaymentSheetPrimaryButton.Text = canPay ? "Pagar con Saldo Paynest" : "Abonar saldo";
		PaymentSheetSecondaryButton.Text = canPay ? "Cancelar" : "Seguir aquí";

		PaymentSheetStatusBadge.BackgroundColor = canPay ? Color.FromArgb("#EBF5EF") : Color.FromArgb("#FEF3C7");
		PaymentSheetStatusLabel.TextColor = canPay ? Color.FromArgb("#2A6349") : Color.FromArgb("#92400E");
		PaymentSheetAfterLabel.TextColor = canPay ? Color.FromArgb("#2A6349") : Color.FromArgb("#991B1B");

		PaymentSheetOverlay.Opacity = 0;
		PaymentSheetPanel.TranslationY = 48;
		PaymentSheetOverlay.IsVisible = true;
		await Task.WhenAll(
			PaymentSheetOverlay.FadeToAsync(1, 150),
			PaymentSheetPanel.TranslateToAsync(0, 0, 220, Easing.CubicOut));

		return await _paymentSheetCompletion.Task;
	}

	private async Task ClosePaymentSheetAsync(bool result)
	{
		var completion = _paymentSheetCompletion;
		if (completion is null)
		{
			return;
		}

		_paymentSheetCompletion = null;
		await Task.WhenAll(
			PaymentSheetPanel.TranslateToAsync(0, 48, 180, Easing.CubicIn),
			PaymentSheetOverlay.FadeToAsync(0, 160));
		PaymentSheetOverlay.IsVisible = false;
		completion.TrySetResult(result);
	}

	private async void OnPaymentSheetPrimaryClicked(object sender, EventArgs e)
	{
		await ClosePaymentSheetAsync(true);
	}

	private async void OnPaymentSheetDismissClicked(object sender, EventArgs e)
	{
		await ClosePaymentSheetAsync(false);
	}

	private static bool ContainsAny(HttpRequestException ex, params string[] values)
	{
		return values.Any(value => ex.Message.Contains(value, StringComparison.OrdinalIgnoreCase));
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_requestCts?.Cancel();
		_requestCts?.Dispose();
		_requestCts = null;
	}

	private async Task LoadGroupSafeAsync(string groupId)
	{
		try
		{
			await _viewModel.LoadAsync(groupId, _requestCts?.Token ?? CancellationToken.None);
		}
		catch (OperationCanceledException)
		{
			// Cambio de pantalla esperado.
		}
	}
}
