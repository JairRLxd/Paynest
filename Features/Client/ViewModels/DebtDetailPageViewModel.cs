#nullable enable
using System.Collections.ObjectModel;
using Paynest.Models;
using Paynest.Services;

namespace Paynest.Features.Client.ViewModels;

public sealed class DebtDetailPageViewModel : BaseViewModel
{
	private readonly IClientDebtService _service;
	private string _headerText = string.Empty;
	private string _subHeaderText = string.Empty;
	private string _loadedGroupId = string.Empty;
	private bool _isPaying;
	private bool _isAllPaidStateVisible;

	public DebtDetailPageViewModel(IClientDebtService service)
	{
		_service = service;
	}

	public ObservableCollection<InstallmentSection> InstallmentSections { get; } = [];
	public string HeaderText
	{
		get => _headerText;
		private set => SetProperty(ref _headerText, value);
	}

	public string SubHeaderText
	{
		get => _subHeaderText;
		private set => SetProperty(ref _subHeaderText, value);
	}

	public string LastPaidInstallmentId { get; private set; } = string.Empty;
	public PayInstallmentResult? LastPaymentResult { get; private set; }
	public string LastPaymentBalanceText => LastPaymentResult?.WalletBalance is decimal balance ? balance.ToString("C") : string.Empty;
	public string LastPaymentMethodText => LastPaymentResult?.PaymentMethod?.Trim().ToLowerInvariant() switch
	{
		"cash"                                                                          => "Efectivo",
		"transfer"                                                                      => "Transferencia",
		"card" or "wallet" or "paynestwallet" or "paynest_wallet" or "paynest_card"    => "Tarjeta Paynest",
		{ Length: > 0 } other                                                           => other,
		_                                                                               => "Tarjeta Paynest"
	};
	public bool IsAllPaidStateVisible
	{
		get => _isAllPaidStateVisible;
		private set => SetProperty(ref _isAllPaidStateVisible, value);
	}
	public bool WasDebtSettledByLastPayment { get; private set; }

	public bool IsPaying
	{
		get => _isPaying;
		private set
		{
			SetProperty(ref _isPaying, value);
			RaisePropertyChanged(nameof(IsNotPaying));
			RaisePropertyChanged(nameof(PayActionText));
		}
	}
	public bool IsNotPaying => !IsPaying;
	public string PayActionText => IsPaying ? "Procesando..." : "Pagar cuota";

	public async Task LoadAsync(string groupId, CancellationToken cancellationToken = default)
	{
		var groups = await _service.GetGroupsAsync(cancellationToken);
		var group = groups.FirstOrDefault(g => g.Id == groupId) ?? _service.CurrentGroup;
		_loadedGroupId = group.Id;
		HeaderText = group.Name;
		SubHeaderText = $"Freelancer: {group.FreelancerName} • {ToDisplayText(group.Frequency)}";

		var installments = await _service.GetInstallmentsByGroupAsync(group.Id, cancellationToken);
		IsAllPaidStateVisible = installments.Count > 0 && installments.All(i => i.Status == InstallmentStatus.Paid);
		var sections = BuildSections(installments);
		InstallmentSections.Clear();
		foreach (var section in sections)
		{
			InstallmentSections.Add(section);
		}
	}

	public Task ReloadAsync(CancellationToken cancellationToken = default)
	{
		var groupId = string.IsNullOrWhiteSpace(_loadedGroupId) ? _service.CurrentGroup.Id : _loadedGroupId;
		return LoadAsync(groupId, cancellationToken);
	}

	public async Task<bool> PayInstallmentAsync(string installmentId, CancellationToken cancellationToken = default)
	{
		if (IsPaying || string.IsNullOrWhiteSpace(installmentId))
		{
			return false;
		}

		try
		{
			IsPaying = true;
			var result = await _service.MarkInstallmentAsPaidAsync(installmentId, cancellationToken);
			if (result.Success)
			{
				LastPaidInstallmentId = installmentId;
				LastPaymentResult = result;
				RaisePropertyChanged(nameof(LastPaymentResult));
				RaisePropertyChanged(nameof(LastPaymentBalanceText));
				RaisePropertyChanged(nameof(LastPaymentMethodText));
				MarkInstallmentAsPaidLocally(installmentId);
				await ReloadAsync(cancellationToken);
				WasDebtSettledByLastPayment = IsAllPaidStateVisible;
				RaisePropertyChanged(nameof(WasDebtSettledByLastPayment));
				return true;
			}

			return false;
		}
		finally
		{
			IsPaying = false;
		}
	}

	private static string ToDisplayText(PaymentFrequency value) => value switch
	{
		PaymentFrequency.Weekly => "Semanal",
		PaymentFrequency.Biweekly => "Quincenal",
		_ => "Mensual"
	};

	private static IReadOnlyList<InstallmentSection> BuildSections(IEnumerable<Installment> installments)
	{
		var orderedInstallments = installments
			.OrderBy(i => i.Status == InstallmentStatus.Paid)
			.ThenBy(i => i.Status == InstallmentStatus.Pending)
			.ThenBy(i => i.DueDate)
			.ToList();

		var sections = new[]
		{
			BuildSection("Vencidas", orderedInstallments.Where(i => i.Status == InstallmentStatus.Overdue)),
			BuildSection("Pendientes", orderedInstallments.Where(i => i.Status == InstallmentStatus.Pending)),
			BuildSection("Pagadas", orderedInstallments.Where(i => i.Status == InstallmentStatus.Paid))
		};

		return sections.Where(section => section.Count > 0).ToList();
	}

	private static InstallmentSection BuildSection(string title, IEnumerable<Installment> installments)
	{
		var section = new InstallmentSection(title);
		foreach (var installment in installments.OrderBy(i => i.DueDate))
		{
			section.Add(new InstallmentRowItem(installment));
		}

		return section;
	}

	private void MarkInstallmentAsPaidLocally(string installmentId)
	{
		foreach (var item in InstallmentSections.SelectMany(section => section))
		{
			if (item.Id == installmentId)
			{
				item.MarkAsPaid();
				return;
			}
		}
	}

	public sealed class InstallmentSection : ObservableCollection<InstallmentRowItem>
	{
		public InstallmentSection(string title)
		{
			Title = title;
		}

		public string Title { get; }
		public string CountText => Count == 1 ? "1 cuota" : $"{Count} cuotas";
	}

	public sealed class InstallmentRowItem : BaseViewModel
	{
		private string _statusText;
		private string _statusBg;
		private string _statusFg;
		private string _statusIcon;
		private bool _showPayButton;

		public InstallmentRowItem(Installment item)
		{
			Id = item.Id;
			Title = item.Title;
			Amount = item.Amount;
			AmountText = item.Amount.ToString("C");
			DueDateText = $"Vence: {item.DueDate:dd MMM yyyy}";
			(_statusText, _statusBg, _statusFg, _statusIcon) = InstallmentStatusPresenter.Present(item.Status);
			_showPayButton = item.Status is InstallmentStatus.Pending or InstallmentStatus.Overdue;
		}

		public string Id { get; }
		public string Title { get; }
		public decimal Amount { get; }
		public string AmountText { get; }
		public string DueDateText { get; }
		public string StatusText
		{
			get => _statusText;
			private set => SetProperty(ref _statusText, value);
		}

		public string StatusBg
		{
			get => _statusBg;
			private set => SetProperty(ref _statusBg, value);
		}

		public string StatusFg
		{
			get => _statusFg;
			private set => SetProperty(ref _statusFg, value);
		}

		public string StatusIcon
		{
			get => _statusIcon;
			private set => SetProperty(ref _statusIcon, value);
		}

		public bool ShowPayButton
		{
			get => _showPayButton;
			private set => SetProperty(ref _showPayButton, value);
		}

		public void MarkAsPaid()
		{
			(StatusText, StatusBg, StatusFg, StatusIcon) = InstallmentStatusPresenter.Present(InstallmentStatus.Paid);
			ShowPayButton = false;
		}
	}
}
