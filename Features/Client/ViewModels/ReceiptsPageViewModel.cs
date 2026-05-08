using System.Collections.ObjectModel;
using System.Windows.Input;
using Paynest.Models;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class ReceiptsPageViewModel : BaseViewModel
{
	private readonly IClientDebtService _service;
	private string _groupText = string.Empty;
	private bool _hasError;
	private string _errorMessage = string.Empty;
	private ScreenState _state = ScreenState.Loading;
	private bool _isRefreshing;

	public ReceiptsPageViewModel(IClientDebtService service)
	{
		_service = service;
		_service.CurrentGroupChanged += OnCurrentGroupChanged;
		RetryCommand = new Command(async () => await RefreshAsync());
		RefreshCommand = new Command(async () => await RefreshFromPullAsync());
	}

	public ObservableCollection<ReceiptSection> ReceiptSections { get; } = [];
	public ObservableCollection<ReceiptItem> Receipts { get; } = [];

	public string GroupText
	{
		get => _groupText;
		private set => SetProperty(ref _groupText, value);
	}

	public bool HasItems => Receipts.Count > 0;
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

	public async Task RefreshAsync(CancellationToken cancellationToken = default)
	{
		State = ScreenState.Loading;
		try
		{
			GroupText = "Recibos emitidos";
			var receipts = await _service.GetReceiptsAsync(cancellationToken);
			var nextReceipts = receipts.Select(r => new ReceiptItem(r)).ToList();
			var nextSections = BuildSections(nextReceipts);
			ReceiptSections.Clear();
			Receipts.Clear();
			foreach (var section in nextSections)
			{
				ReceiptSections.Add(section);
			}
			foreach (var receipt in nextReceipts.OrderByDescending(x => x.PaidDate))
			{
				Receipts.Add(receipt);
			}
			HasError = false;
			ErrorMessage = string.Empty;
			State = Receipts.Count == 0 ? ScreenState.Empty : ScreenState.Content;
		}
		catch (OperationCanceledException)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				// A cancellation triggered by navigation/refresh should not leave the screen in Loading.
				State = Receipts.Count == 0 ? ScreenState.Empty : ScreenState.Content;
			}
			else
			{
				ReceiptSections.Clear();
				Receipts.Clear();
				HasError = true;
				ErrorMessage = "La solicitud tardó demasiado. Intenta nuevamente.";
				State = ScreenState.Error;
			}
		}
		catch (Exception)
		{
			ReceiptSections.Clear();
			Receipts.Clear();
			HasError = true;
			ErrorMessage = "No fue posible cargar tus recibos por ahora. Reintenta en unos segundos.";
			State = ScreenState.Error;
		}
		RaisePropertyChanged(nameof(HasItems));
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

	private static IReadOnlyList<ReceiptSection> BuildSections(IEnumerable<ReceiptItem> receipts)
	{
		return receipts
			.OrderByDescending(receipt => receipt.PaidDate)
			.GroupBy(receipt => receipt.SectionTitle)
			.Select(group =>
			{
				var section = new ReceiptSection(group.Key);
				foreach (var receipt in group)
				{
					section.Add(receipt);
				}

				return section;
			})
			.ToList();
	}

	public sealed class ReceiptSection : ObservableCollection<ReceiptItem>
	{
		public ReceiptSection(string title)
		{
			Title = title;
		}

		public string Title { get; }
		public string CountText => Count == 1 ? "1 recibo" : $"{Count} recibos";
	}

	public sealed class ReceiptItem
	{
		public ReceiptItem(Receipt item)
		{
			Id = item.Id;
			Title = item.Title;
			AmountText = item.Amount.ToString("C");
			PaidDate = item.PaidAt;
			PaidDateText = $"Pagado: {PaidDate:dd MMM yyyy}";
			FolioText = $"Folio: {item.Folio}";
			SectionTitle = PaidDate.ToString("MMMM yyyy");
		}

		public string Id { get; }
		public string Title { get; }
		public string AmountText { get; }
		public DateTime PaidDate { get; }
		public string PaidDateText { get; }
		public string FolioText { get; }
		public string SectionTitle { get; }
	}
}
