using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Paynest.Models;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class CalendarPageViewModel : BaseViewModel
{
	private static readonly CultureInfo EsMxCulture = CultureInfo.GetCultureInfo("es-MX");
	private const string CalendarFilterPreferenceKey = "client.calendar.status_filter";
	private const string CalendarSelectedMonthPreferenceKey = "client.calendar.selected_month";
	private readonly IClientDebtService _service;
	private DateTime _selectedDate = DateTime.Today;
	private string _filterText = string.Empty;
	private bool _hasError;
	private string _errorMessage = string.Empty;
	private ScreenState _state = ScreenState.Loading;
	private bool _isRefreshing;
	private CalendarStatusFilter _statusFilter = CalendarStatusFilter.All;

	public CalendarPageViewModel(IClientDebtService service)
	{
		_service = service;
		_statusFilter = LoadSavedStatusFilter();
		_selectedDate = LoadSavedSelectedMonth();
		RetryCommand = new Command(async () => await RefreshAsync());
		RefreshCommand = new Command(async () => await RefreshFromPullAsync());
		SetStatusFilterCommand = new Command<string>(async value => await SetStatusFilterAsync(value));
	}

	public ObservableCollection<CalendarInstallmentSection> InstallmentSections { get; } = [];

	public DateTime SelectedDate
	{
		get => _selectedDate;
		set
		{
			var normalized = new DateTime(value.Year, value.Month, 1);
			if (_selectedDate == normalized)
			{
				return;
			}

			SetProperty(ref _selectedDate, normalized);
			Preferences.Default.Set(CalendarSelectedMonthPreferenceKey, normalized.ToString("yyyy-MM"));
			_ = RefreshAsync();
		}
	}

	public string FilterText
	{
		get => _filterText;
		private set => SetProperty(ref _filterText, value);
	}

	public bool HasItems => InstallmentSections.Count > 0;
	public bool HasError
	{
		get => _hasError;
		private set => SetProperty(ref _hasError, value);
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		private set => SetProperty(ref _errorMessage, value);
	}

	public ICommand RetryCommand { get; }
	public ICommand RefreshCommand { get; }
	public ICommand SetStatusFilterCommand { get; }
	public ScreenState State
	{
		get => _state;
		private set => SetProperty(ref _state, value);
	}
	public bool IsRefreshing
	{
		get => _isRefreshing;
		private set => SetProperty(ref _isRefreshing, value);
	}

	public bool IsAllFilterSelected => _statusFilter == CalendarStatusFilter.All;
	public bool IsPendingFilterSelected => _statusFilter == CalendarStatusFilter.Pending;
	public bool IsOverdueFilterSelected => _statusFilter == CalendarStatusFilter.Overdue;
	public bool IsPaidFilterSelected => _statusFilter == CalendarStatusFilter.Paid;
	public string EmptyTitleText => _statusFilter switch
	{
		CalendarStatusFilter.Pending => "Sin pagos pendientes este mes",
		CalendarStatusFilter.Overdue => "Sin cuotas vencidas",
		CalendarStatusFilter.Paid => "Sin cuotas pagadas este mes",
		_ => "Sin vencimientos este mes"
	};
	public string EmptyDescriptionText => _statusFilter switch
	{
		CalendarStatusFilter.Pending => "No hay pagos pendientes para el periodo seleccionado.",
		CalendarStatusFilter.Overdue => "Vas al día en este periodo. No hay cuotas atrasadas.",
		CalendarStatusFilter.Paid => "Cuando una cuota se pague en este mes, aparecerá aquí.",
		_ => "Cuando existan cuotas en este periodo, aparecerán aquí."
	};

	public async Task RefreshAsync(CancellationToken cancellationToken = default)
	{
		State = ScreenState.Loading;
		try
		{
			var month = SelectedDate.Month;
			var year = SelectedDate.Year;
			FilterText = $"Mostrando: {SelectedDate.ToString("MMMM yyyy", EsMxCulture)}";

			var groups = await _service.GetGroupsAsync(cancellationToken);
			var installmentTasks = groups
				.Select(async group =>
				{
					var groupInstallments = await _service.GetInstallmentsByGroupAsync(group.Id, cancellationToken);
					return groupInstallments.Select(i => (Group: group, Installment: i));
				})
				.ToList();
			var allInstallments = (await Task.WhenAll(installmentTasks))
				.SelectMany(items => items)
				.ToList();

			var monthlyInstallments = allInstallments
				.Where(x => x.Installment.DueDate.Month == month && x.Installment.DueDate.Year == year)
				.OrderBy(x => x.Installment.DueDate)
				.ToList();
			var sourceInstallments = _statusFilter == CalendarStatusFilter.All
				? allInstallments.OrderBy(x => x.Installment.DueDate).ToList()
				: monthlyInstallments;
			var filteredInstallments = ApplyStatusFilter(sourceInstallments).ToList();

			var nextInstallments = filteredInstallments
				.Select(item => new CalendarInstallmentItem(item.Group.Name, item.Installment))
				.ToList();
			ReplaceSections(nextInstallments);

			HasError = false;
			ErrorMessage = string.Empty;
			State = InstallmentSections.Count == 0 ? ScreenState.Empty : ScreenState.Content;
		}
		catch (OperationCanceledException)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				InstallmentSections.Clear();
				HasError = true;
				ErrorMessage = "La solicitud tardó demasiado. Intenta nuevamente.";
				State = ScreenState.Error;
			}
		}
		catch (Exception)
		{
			InstallmentSections.Clear();
			HasError = true;
			ErrorMessage = "Tuvimos un problema al cargar tus vencimientos. Intenta nuevamente.";
			State = ScreenState.Error;
		}
		RaisePropertyChanged(nameof(HasItems));
	}

	private async Task SetStatusFilterAsync(string value)
	{
		var nextFilter = value switch
		{
			"Pending" => CalendarStatusFilter.Pending,
			"Overdue" => CalendarStatusFilter.Overdue,
			"Paid" => CalendarStatusFilter.Paid,
			_ => CalendarStatusFilter.All
		};

		if (_statusFilter == nextFilter)
		{
			return;
		}

		_statusFilter = nextFilter;
		Preferences.Default.Set(CalendarFilterPreferenceKey, ToPreferenceValue(_statusFilter));
		RaiseFilterSelectionChanged();
		await RefreshAsync();
	}

	private static CalendarStatusFilter LoadSavedStatusFilter()
	{
		var saved = Preferences.Default.Get(CalendarFilterPreferenceKey, "All");
		return saved switch
		{
			"Pending" => CalendarStatusFilter.Pending,
			"Overdue" => CalendarStatusFilter.Overdue,
			"Paid" => CalendarStatusFilter.Paid,
			_ => CalendarStatusFilter.All
		};
	}

	private static string ToPreferenceValue(CalendarStatusFilter filter)
	{
		return filter switch
		{
			CalendarStatusFilter.Pending => "Pending",
			CalendarStatusFilter.Overdue => "Overdue",
			CalendarStatusFilter.Paid => "Paid",
			_ => "All"
		};
	}

	private static DateTime LoadSavedSelectedMonth()
	{
		var saved = Preferences.Default.Get(CalendarSelectedMonthPreferenceKey, string.Empty);
		return DateTime.TryParse($"{saved}-01", out var parsed)
			? new DateTime(parsed.Year, parsed.Month, 1)
			: new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
	}

	private IEnumerable<(DebtGroup Group, Installment Installment)> ApplyStatusFilter(IEnumerable<(DebtGroup Group, Installment Installment)> installments)
	{
		return _statusFilter switch
		{
			CalendarStatusFilter.Pending => installments.Where(x => x.Installment.Status == InstallmentStatus.Pending),
			CalendarStatusFilter.Overdue => installments.Where(x => x.Installment.Status == InstallmentStatus.Overdue),
			CalendarStatusFilter.Paid => installments.Where(x => x.Installment.Status == InstallmentStatus.Paid),
			_ => installments
		};
	}

	private void ReplaceSections(IReadOnlyList<CalendarInstallmentItem> installments)
	{
		InstallmentSections.Clear();
		foreach (var group in installments
			.OrderBy(item => item.DueDate)
			.GroupBy(GetSectionTitle)
			.OrderBy(group => GetSectionOrder(group.Key)))
		{
			var section = new CalendarInstallmentSection(group.Key);
			foreach (var item in group.OrderBy(item => item.DueDate))
			{
				section.Add(item);
			}

			InstallmentSections.Add(section);
		}
	}

	private static string GetSectionTitle(CalendarInstallmentItem item)
	{
		var today = DateTime.Today;
		var dueDate = item.DueDate.Date;
		if (dueDate == today)
		{
			return "Hoy";
		}

		if (dueDate > today && dueDate <= today.AddDays(7))
		{
			return "Esta semana";
		}

		if (dueDate.Month == today.Month && dueDate.Year == today.Year)
		{
			return "Este mes";
		}

		return "Más adelante";
	}

	private static int GetSectionOrder(string title)
	{
		return title switch
		{
			"Hoy" => 0,
			"Esta semana" => 1,
			"Este mes" => 2,
			_ => 3
		};
	}

	private void RaiseFilterSelectionChanged()
	{
		RaisePropertyChanged(nameof(IsAllFilterSelected));
		RaisePropertyChanged(nameof(IsPendingFilterSelected));
		RaisePropertyChanged(nameof(IsOverdueFilterSelected));
		RaisePropertyChanged(nameof(IsPaidFilterSelected));
		RaisePropertyChanged(nameof(EmptyTitleText));
		RaisePropertyChanged(nameof(EmptyDescriptionText));
	}

	private enum CalendarStatusFilter
	{
		All,
		Pending,
		Overdue,
		Paid
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

	public sealed class CalendarInstallmentItem
	{
		public CalendarInstallmentItem(string groupName, Installment item)
		{
			Id = item.Id;
			DueDate = item.DueDate;
			DayText = item.DueDate.ToString("dd");
			Title = item.Title;
			AmountText = item.Amount.ToString("C");
			GroupText = groupName;
			(StatusText, StatusBg, StatusFg, StatusIcon) = InstallmentStatusPresenter.Present(item.Status);
		}

		public string Id { get; }
		public DateTime DueDate { get; }
		public string DayText { get; }
		public string Title { get; }
		public string AmountText { get; }
		public string GroupText { get; }
		public string StatusText { get; }
		public string StatusBg { get; }
		public string StatusFg { get; }
		public string StatusIcon { get; }
	}

	public sealed class CalendarInstallmentSection : ObservableCollection<CalendarInstallmentItem>
	{
		public CalendarInstallmentSection(string title)
		{
			Title = title;
		}

		public string Title { get; }
		public string CountText => Count == 1 ? "1 cuota" : $"{Count} cuotas";
	}
}
