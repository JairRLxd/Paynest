using System.Collections.ObjectModel;
using Paynest.Features.Client.Api;
using Paynest.Models;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class MainPageViewModel : BaseViewModel
{
	private readonly IClientDebtService _service;
	private readonly IDebtApiClient _debtApiClient;
	private string _currentGroupText = string.Empty;
	private string _activeGroupsText = "0";
	private string _nextPaymentText = "Sin pagos próximos";
	private string _overdueCountText = "0";
	private string _totalPendingText = "$0.00";
	private string _walletBalanceText = "$0.00";
	private string _walletMovementText = "Sin movimientos recientes";
	private string _walletMovementAmountText = string.Empty;
	private string _walletMovementAmountColor = "#6B6B6B";
	private bool _hasWalletMovement;
	private ScreenState _state = ScreenState.Loading;
	private string _errorMessage = string.Empty;
	private bool _isRefreshing;

	public MainPageViewModel(IClientDebtService service, IDebtApiClient debtApiClient)
	{
		_service = service;
		_debtApiClient = debtApiClient;
		_service.CurrentGroupChanged += OnCurrentGroupChanged;
		RetryCommand = new Command(async () => await RefreshAsync());
		RefreshCommand = new Command(async () => await RefreshFromPullAsync());
		LinkCollectorCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LinkCollectorPage)));
		LoadLocalSnapshot();
	}

	public ObservableCollection<GroupCardItem> Groups { get; } = [];
	public ObservableCollection<InstallmentCardItem> UpcomingInstallments { get; } = [];
	public Command RetryCommand { get; }
	public Command RefreshCommand { get; }
	public Command LinkCollectorCommand { get; }

	public string CurrentGroupText
	{
		get => _currentGroupText;
		private set => SetProperty(ref _currentGroupText, value);
	}

	public string ActiveGroupsText
	{
		get => _activeGroupsText;
		private set => SetProperty(ref _activeGroupsText, value);
	}

	public string NextPaymentText
	{
		get => _nextPaymentText;
		private set => SetProperty(ref _nextPaymentText, value);
	}

	public string OverdueCountText
	{
		get => _overdueCountText;
		private set => SetProperty(ref _overdueCountText, value);
	}

	public string TotalPendingText
	{
		get => _totalPendingText;
		private set => SetProperty(ref _totalPendingText, value);
	}

	public string WalletBalanceText
	{
		get => _walletBalanceText;
		private set => SetProperty(ref _walletBalanceText, value);
	}

	public string WalletMovementText
	{
		get => _walletMovementText;
		private set => SetProperty(ref _walletMovementText, value);
	}

	public string WalletMovementAmountText
	{
		get => _walletMovementAmountText;
		private set => SetProperty(ref _walletMovementAmountText, value);
	}

	public string WalletMovementAmountColor
	{
		get => _walletMovementAmountColor;
		private set => SetProperty(ref _walletMovementAmountColor, value);
	}

	public bool HasWalletMovement
	{
		get => _hasWalletMovement;
		private set => SetProperty(ref _hasWalletMovement, value);
	}

	public ScreenState State
	{
		get => _state;
		private set => SetProperty(ref _state, value);
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		private set => SetProperty(ref _errorMessage, value);
	}

	public bool IsRefreshing
	{
		get => _isRefreshing;
		private set => SetProperty(ref _isRefreshing, value);
	}

	public async Task RefreshAsync(CancellationToken cancellationToken = default)
	{
		State = ScreenState.Loading;
		try
		{
			var groups = await _service.GetGroupsAsync(cancellationToken);
			var currentInstallments = await _service.GetCurrentGroupInstallmentsAsync(cancellationToken);
			ApplyDashboard(groups, currentInstallments);
			await RefreshWalletSummaryAsync(cancellationToken);
			ErrorMessage = string.Empty;
			State = (Groups.Count == 0 && UpcomingInstallments.Count == 0) ? ScreenState.Empty : ScreenState.Content;
		}
		catch (OperationCanceledException)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				ErrorMessage = "La solicitud tardó demasiado. Intenta nuevamente.";
				State = ScreenState.Error;
			}
		}
		catch (Exception)
		{
			ErrorMessage = "No pudimos cargar tus deudas por ahora. Intenta nuevamente.";
			State = (Groups.Count == 0 && UpcomingInstallments.Count == 0) ? ScreenState.Error : ScreenState.Content;
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
			await RefreshAsync(cancellationToken);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	public void Dispose()
	{
		_service.CurrentGroupChanged -= OnCurrentGroupChanged;
	}

	private void OnCurrentGroupChanged(object? sender, EventArgs e)
	{
		_ = RefreshAsync();
	}

	private void LoadLocalSnapshot()
	{
		try
		{
			ApplyDashboard(_service.GetGroups(), _service.GetCurrentGroupInstallments());
			State = (Groups.Count == 0 && UpcomingInstallments.Count == 0) ? ScreenState.Empty : ScreenState.Content;
		}
		catch
		{
			State = ScreenState.Loading;
		}
	}

	private async Task RefreshWalletSummaryAsync(CancellationToken cancellationToken)
	{
		try
		{
			var walletTask = _debtApiClient.GetWalletAsync(cancellationToken);
			var walletMovementsTask = _debtApiClient.GetWalletMovementsAsync(limit: 1, cancellationToken);
			await Task.WhenAll(walletTask, walletMovementsTask);
			ApplyWallet(walletTask.Result, walletMovementsTask.Result);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			WalletMovementText = "Saldo no actualizado";
			WalletMovementAmountText = string.Empty;
			HasWalletMovement = false;
		}
	}

	private void ApplyDashboard(IReadOnlyList<DebtGroup> groups, IReadOnlyList<Installment> currentInstallments)
	{
		var nextGroups = groups.Select(g => new GroupCardItem(g)).ToList();
		CollectionSyncHelper.SyncByKey(Groups, nextGroups, x => x.Id);

		CurrentGroupText = $"Grupo actual: {_service.CurrentGroup.Name}";
		ActiveGroupsText = groups.Count.ToString();
		TotalPendingText = groups.Sum(g => g.PendingAmount).ToString("C");

		var actionableInstallments = currentInstallments
			.Where(i => i.Status is InstallmentStatus.Pending or InstallmentStatus.Overdue)
			.OrderBy(i => i.DueDate)
			.ToList();
		var nextPayment = actionableInstallments.FirstOrDefault();
		NextPaymentText = nextPayment is null ? "Sin pagos próximos" : $"{nextPayment.Amount:C} · {nextPayment.DueDate:dd MMM}";
		OverdueCountText = currentInstallments.Count(i => i.Status == InstallmentStatus.Overdue).ToString();

		var nextUpcoming = actionableInstallments.Take(4).Select(i => new InstallmentCardItem(i)).ToList();
		CollectionSyncHelper.SyncByKey(UpcomingInstallments, nextUpcoming, x => x.Id);
	}

	private void ApplyWallet(WalletDto wallet, IReadOnlyList<WalletMovementDto> movements)
	{
		WalletBalanceText = wallet.Balance.ToString("C");
		var movement = movements.FirstOrDefault();
		HasWalletMovement = movement is not null;
		if (movement is null)
		{
			WalletMovementText = "Sin movimientos recientes";
			WalletMovementAmountText = string.Empty;
			WalletMovementAmountColor = "#6B6B6B";
			return;
		}

		WalletMovementText = string.IsNullOrWhiteSpace(movement.Description)
			? ToMovementTypeText(movement.Type)
			: movement.Description;
		WalletMovementAmountText = movement.Amount.ToString("C");
		WalletMovementAmountColor = movement.Amount >= 0 ? "#2A6349" : "#991B1B";
	}

	private static string ToMovementTypeText(string type)
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

	public sealed class GroupCardItem
	{
		public GroupCardItem(DebtGroup group)
		{
			Id = group.Id;
			Name = string.IsNullOrWhiteSpace(group.Name) ? "Deuda activa" : group.Name;
			FreelancerName = string.IsNullOrWhiteSpace(group.FreelancerName)
				? "Freelancer: Sin asignar"
				: $"Freelancer: {group.FreelancerName}";
			FrequencyText = $"Frecuencia: {ToDisplayText(group.Frequency)}";
			PendingAmountText = group.PendingAmount.ToString("C");
		}

		public string Id { get; }
		public string Name { get; }
		public string FreelancerName { get; }
		public string FrequencyText { get; }
		public string PendingAmountText { get; }

		private static string ToDisplayText(PaymentFrequency value) => value switch
		{
			PaymentFrequency.Weekly => "Semanal",
			PaymentFrequency.Biweekly => "Quincenal",
			_ => "Mensual"
		};
	}

	public sealed class InstallmentCardItem
	{
		public InstallmentCardItem(Installment item)
		{
			Id = item.Id;
			Title = item.Title;
			AmountText = item.Amount.ToString("C");
			DueDateText = $"Vence: {item.DueDate:dd MMM yyyy}";
			(StatusText, StatusBg, StatusFg, StatusIcon) = InstallmentStatusPresenter.Present(item.Status);
		}

		public string Id { get; }
		public string Title { get; }
		public string AmountText { get; }
		public string DueDateText { get; }
		public string StatusText { get; }
		public string StatusBg { get; }
		public string StatusFg { get; }
		public string StatusIcon { get; }
	}
}
