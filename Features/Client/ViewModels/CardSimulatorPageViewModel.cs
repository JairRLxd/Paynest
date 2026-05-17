#nullable enable
using System.Collections.ObjectModel;
using Paynest.Features.Client.Api;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class CardSimulatorPageViewModel : BaseViewModel
{
	private readonly IDebtApiClient _apiClient;
	private string _balanceText = "$0.00";
	private string _description = string.Empty;
	private string _amountInput = string.Empty;
	private string _validationError = string.Empty;
	private string _errorMessage = string.Empty;
	private string _lastUpdatedText = "Sin actualizar";
	private string _latestMovementTitle = "Sin movimientos recientes";
	private string _latestMovementAmountText = string.Empty;
	private string _latestMovementMetaText = "Abona saldo o paga una cuota para ver actividad.";
	private string _latestMovementAmountColor = "#6B6B6B";
	private bool _isLoading;
	private bool _isDepositing;
	private bool _isRefreshing;
	private bool _hasLatestMovement;
	private string _highlightedMovementId = string.Empty;

	public CardSimulatorPageViewModel(IDebtApiClient apiClient)
	{
		_apiClient = apiClient;
		RefreshCommand = new Command(async () => await RefreshFromPullAsync());
	}

	public ObservableCollection<CardMovementItem> Movements { get; } = [];
	public Command RefreshCommand { get; }
	public string LastCreatedMovementId { get; private set; } = string.Empty;

	public string BalanceText
	{
		get => _balanceText;
		private set => SetProperty(ref _balanceText, value);
	}

	public bool HasMovements => Movements.Count > 0;
	public bool HasNoMovements => !HasMovements;
	public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);
	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
	public bool IsNotBusy => !IsLoading && !IsDepositing;
	public bool IsNotLoading => !IsLoading;
	public string DepositButtonText => IsDepositing ? "Abonando..." : "Abonar saldo";
	public string LastUpdatedText
	{
		get => _lastUpdatedText;
		private set => SetProperty(ref _lastUpdatedText, value);
	}

	public bool HasLatestMovement
	{
		get => _hasLatestMovement;
		private set => SetProperty(ref _hasLatestMovement, value);
	}

	public string LatestMovementTitle
	{
		get => _latestMovementTitle;
		private set => SetProperty(ref _latestMovementTitle, value);
	}

	public string LatestMovementAmountText
	{
		get => _latestMovementAmountText;
		private set => SetProperty(ref _latestMovementAmountText, value);
	}

	public string LatestMovementMetaText
	{
		get => _latestMovementMetaText;
		private set => SetProperty(ref _latestMovementMetaText, value);
	}

	public string LatestMovementAmountColor
	{
		get => _latestMovementAmountColor;
		private set => SetProperty(ref _latestMovementAmountColor, value);
	}

	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			SetProperty(ref _isLoading, value);
			RaisePropertyChanged(nameof(IsNotBusy));
			RaisePropertyChanged(nameof(IsNotLoading));
		}
	}

	public bool IsDepositing
	{
		get => _isDepositing;
		private set
		{
			SetProperty(ref _isDepositing, value);
			RaisePropertyChanged(nameof(IsNotBusy));
			RaisePropertyChanged(nameof(DepositButtonText));
		}
	}

	public bool IsRefreshing
	{
		get => _isRefreshing;
		private set => SetProperty(ref _isRefreshing, value);
	}

	public string Description
	{
		get => _description;
		set
		{
			SetProperty(ref _description, value);
			ClearValidationError();
		}
	}

	public string AmountInput
	{
		get => _amountInput;
		set
		{
			SetProperty(ref _amountInput, value);
			ClearValidationError();
		}
	}

	public string ValidationError
	{
		get => _validationError;
		private set
		{
			SetProperty(ref _validationError, value);
			RaisePropertyChanged(nameof(HasValidationError));
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

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		if (IsLoading)
		{
			return;
		}

		try
		{
			IsLoading = true;
			ErrorMessage = string.Empty;
			var walletTask = _apiClient.GetWalletAsync(cancellationToken);
			var movementsTask = _apiClient.GetWalletMovementsAsync(cancellationToken: cancellationToken);
			await Task.WhenAll(walletTask, movementsTask);

			ApplyWallet(walletTask.Result);
			ReplaceMovements(movementsTask.Result);
			MarkUpdatedNow();
		}
		catch (OperationCanceledException)
		{
			// Navegación esperada.
		}
		catch (Exception ex)
		{
			ErrorMessage = NetworkErrorMessageProvider.From(ex);
		}
		finally
		{
			IsLoading = false;
		}
	}

	public async Task RefreshFromPullAsync(CancellationToken cancellationToken = default)
	{
		if (IsRefreshing)
		{
			return;
		}

		try
		{
			IsRefreshing = true;
			await LoadAsync(cancellationToken);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	public async Task<bool> DepositAsync(CancellationToken cancellationToken = default)
	{
		if (IsDepositing || !TryGetAmount(out var amount))
		{
			return false;
		}

		try
		{
			IsDepositing = true;
			ErrorMessage = string.Empty;
			var response = await _apiClient.DepositWalletAsync(new WalletDepositRequestDto
			{
				Amount = amount,
				Description = string.IsNullOrWhiteSpace(Description) ? "Abono de saldo" : Description.Trim()
			}, cancellationToken);

			ApplyWallet(response.Wallet);
			Movements.Insert(0, new CardMovementItem(response.Movement));
			ApplyLatestMovement(Movements.FirstOrDefault());
			MarkUpdatedNow();
			LastCreatedMovementId = response.Movement.Id;
			AmountInput = string.Empty;
			Description = string.Empty;
			RaiseMovementStateChanged();
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
			IsDepositing = false;
		}
	}

	public async Task HighlightMovementAsync(string movementId)
	{
		if (string.IsNullOrWhiteSpace(movementId))
		{
			return;
		}

		_highlightedMovementId = movementId;
		ApplyHighlightedMovement();
		await PulseHighlightedMovementAsync(movementId);
		await Task.Delay(3200);
		if (_highlightedMovementId == movementId)
		{
			_highlightedMovementId = string.Empty;
			ApplyHighlightedMovement();
		}
	}

	private async Task PulseHighlightedMovementAsync(string movementId)
	{
		var movement = Movements.FirstOrDefault(item => item.Id == movementId);
		if (movement is null)
		{
			return;
		}

		movement.CardScale = 1.015;
		await Task.Delay(140);
		movement.CardScale = 1;
	}

	private bool TryGetAmount(out decimal amount)
	{
		amount = 0m;
		ValidationError = string.Empty;
		if (string.IsNullOrWhiteSpace(AmountInput) || !decimal.TryParse(AmountInput.Trim(), out amount) || amount <= 0)
		{
			ValidationError = "Ingresa un monto numérico mayor a 0.";
			return false;
		}

		return true;
	}

	private void ApplyWallet(WalletDto wallet)
	{
		BalanceText = wallet.Balance.ToString("C");
	}

	private void MarkUpdatedNow()
	{
		LastUpdatedText = $"Actualizado {DateTime.Now:HH:mm}";
	}

	private void ReplaceMovements(IEnumerable<WalletMovementDto> movements)
	{
		Movements.Clear();
		foreach (var movement in movements)
		{
			Movements.Add(new CardMovementItem(movement));
		}

		ApplyHighlightedMovement();
		ApplyLatestMovement(Movements.FirstOrDefault());
		RaiseMovementStateChanged();
	}

	private void ApplyLatestMovement(CardMovementItem? movement)
	{
		HasLatestMovement = movement is not null;
		LatestMovementTitle = movement?.Description ?? "Sin movimientos recientes";
		LatestMovementAmountText = movement?.AmountText ?? string.Empty;
		LatestMovementMetaText = movement is null
			? "Abona saldo o paga una cuota para ver actividad."
			: $"{movement.TypeText} · {movement.DateText}";
		LatestMovementAmountColor = movement?.AmountColor ?? "#6B6B6B";
	}

	private void ApplyHighlightedMovement()
	{
		foreach (var movement in Movements)
		{
			movement.IsHighlighted = movement.Id == _highlightedMovementId;
		}
	}

	private void RaiseMovementStateChanged()
	{
		RaisePropertyChanged(nameof(HasMovements));
		RaisePropertyChanged(nameof(HasNoMovements));
	}

	private void ClearValidationError()
	{
		if (!string.IsNullOrWhiteSpace(ValidationError))
		{
			ValidationError = string.Empty;
		}
	}

	public sealed class CardMovementItem : BaseViewModel
	{
		private bool _isHighlighted;
		private double _cardScale = 1;

		public CardMovementItem(WalletMovementDto movement)
		{
			Id = movement.Id;
			Description = string.IsNullOrWhiteSpace(movement.Description) ? ToTypeText(movement.Type) : movement.Description;
			AmountText = movement.Amount.ToString("C");
			DateText = movement.CreatedAt.ToString("dd MMM yyyy, HH:mm");
			TypeText = ToTypeText(movement.Type);
			AmountColor = movement.Amount >= 0 ? "#2A6349" : "#991B1B";
			BadgeBackground = movement.Amount >= 0 ? "#EBF5EF" : "#FEE2E2";
			BadgeForeground = movement.Amount >= 0 ? "#2A6349" : "#991B1B";
			ReferenceText = string.IsNullOrWhiteSpace(movement.Reference) ? string.Empty : $"Ref. {movement.Reference}";
		}

		public string Id { get; }
		public string Description { get; }
		public string AmountText { get; }
		public string DateText { get; }
		public string TypeText { get; }
		public string AmountColor { get; }
		public string BadgeBackground { get; }
		public string BadgeForeground { get; }
		public string ReferenceText { get; }
		public Color CardBackground => ResolveThemeColor(
			light: IsHighlighted ? "#EBF5EF" : "#FFFFFF",
			dark: IsHighlighted ? "#25312C" : "#161D1A");
		public Color CardBorderColor => ResolveThemeColor(
			light: IsHighlighted ? "#2A6349" : "#D8D5CE",
			dark: IsHighlighted ? "#39A370" : "#25312C");
		public float CardBorderWidth => IsHighlighted ? 1.5f : 0f;
		public bool IsNewBadgeVisible => IsHighlighted;
		public double CardScale
		{
			get => _cardScale;
			set => SetProperty(ref _cardScale, value);
		}

		public bool IsHighlighted
		{
			get => _isHighlighted;
			set
			{
				if (_isHighlighted == value)
				{
					return;
				}

				SetProperty(ref _isHighlighted, value);
				RaisePropertyChanged(nameof(CardBackground));
				RaisePropertyChanged(nameof(CardBorderColor));
				RaisePropertyChanged(nameof(CardBorderWidth));
				RaisePropertyChanged(nameof(IsNewBadgeVisible));
			}
		}

		private static string ToTypeText(string type)
		{
			return type.Trim().ToLowerInvariant() switch
			{
				"deposit" => "Abono",
				"installment_payment" => "Pago",
				"adjustment" => "Ajuste",
				"refund" => "Reembolso",
				_ => "Movimiento"
			};
		}

		private static Color ResolveThemeColor(string light, string dark)
			=> Color.FromArgb(Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark ? dark : light);
	}
}
