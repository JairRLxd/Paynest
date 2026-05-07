#nullable enable
using Paynest.Features.Client.Repositories;
using Paynest.Models;

namespace Paynest.Services;

public sealed class ClientDebtService : IClientDebtService
{
	private readonly IClientDebtRepository _repository;

	public event EventHandler? CurrentGroupChanged;

	public ClientDebtService(IClientDebtRepository repository)
	{
		_repository = repository;
		_repository.CurrentGroupChanged += OnRepositoryCurrentGroupChanged;
	}

	public DebtGroup CurrentGroup => _repository.CurrentGroup;

	public IReadOnlyList<DebtGroup> GetGroups() => _repository.GetGroups();

	public Task<IReadOnlyList<DebtGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
		=> _repository.GetGroupsAsync(cancellationToken);

	public void SetCurrentGroup(string groupId)
	{
		_repository.SetCurrentGroup(groupId);
	}

	public IReadOnlyList<Installment> GetInstallmentsByGroup(string groupId)
		=> _repository.GetInstallmentsByGroup(groupId);

	public Task<IReadOnlyList<Installment>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default)
		=> _repository.GetInstallmentsByGroupAsync(groupId, cancellationToken);

	public IReadOnlyList<Installment> GetCurrentGroupInstallments()
	{
		if (string.IsNullOrWhiteSpace(_repository.CurrentGroup.Id))
		{
			return [];
		}

		return _repository.GetInstallmentsByGroup(_repository.CurrentGroup.Id)
			.Where(i => i.Status is InstallmentStatus.Pending or InstallmentStatus.Overdue)
			.OrderBy(i => i.DueDate)
			.ToList();
	}

	public async Task<IReadOnlyList<Installment>> GetCurrentGroupInstallmentsAsync(CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_repository.CurrentGroup.Id))
		{
			return [];
		}

		var items = await _repository.GetInstallmentsByGroupAsync(_repository.CurrentGroup.Id, cancellationToken);
		return items
			.Where(i => i.Status is InstallmentStatus.Pending or InstallmentStatus.Overdue)
			.OrderBy(i => i.DueDate)
			.ToList();
	}

	public IReadOnlyList<Installment> GetPaidInstallmentsFromCurrentGroup()
	{
		if (string.IsNullOrWhiteSpace(_repository.CurrentGroup.Id))
		{
			return [];
		}

		return _repository.GetInstallmentsByGroup(_repository.CurrentGroup.Id)
			.Where(i => i.Status == InstallmentStatus.Paid)
			.OrderByDescending(i => i.DueDate)
			.ToList();
	}

	public async Task<IReadOnlyList<Installment>> GetPaidInstallmentsFromCurrentGroupAsync(CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_repository.CurrentGroup.Id))
		{
			return [];
		}

		var items = await _repository.GetInstallmentsByGroupAsync(_repository.CurrentGroup.Id, cancellationToken);
		return items
			.Where(i => i.Status == InstallmentStatus.Paid)
			.OrderByDescending(i => i.DueDate)
			.ToList();
	}

	public bool MarkInstallmentAsPaid(string installmentId)
		=> _repository.MarkInstallmentAsPaid(installmentId);

	public Task<PayInstallmentResult> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default)
		=> _repository.MarkInstallmentAsPaidAsync(installmentId, cancellationToken);

	public Task<IReadOnlyList<Receipt>> GetReceiptsAsync(CancellationToken cancellationToken = default)
		=> _repository.GetReceiptsAsync(cancellationToken);

	public Task<Receipt?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default)
		=> _repository.GetReceiptAsync(receiptId, cancellationToken);

	public Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default)
		=> _repository.GetReceiptDownloadUrlAsync(receiptId, cancellationToken);

	public Task<NotificationPreferences> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default)
		=> _repository.GetNotificationPreferencesAsync(cancellationToken);

	public Task<NotificationPreferences> UpdateNotificationPreferencesAsync(
		NotificationPreferences preferences,
		CancellationToken cancellationToken = default)
		=> _repository.UpdateNotificationPreferencesAsync(preferences, cancellationToken);

	private void OnRepositoryCurrentGroupChanged(object? sender, EventArgs e)
	{
		CurrentGroupChanged?.Invoke(this, EventArgs.Empty);
	}
}
