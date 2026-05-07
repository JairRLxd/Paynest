#nullable enable
using Paynest.Models;

namespace Paynest.Features.Client.Repositories;

public interface IClientDebtRepository
{
	event EventHandler? CurrentGroupChanged;
	DebtGroup CurrentGroup { get; }
	IReadOnlyList<DebtGroup> GetGroups();
	Task<IReadOnlyList<DebtGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);
	void SetCurrentGroup(string groupId);
	IReadOnlyList<Installment> GetInstallmentsByGroup(string groupId);
	Task<IReadOnlyList<Installment>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default);
	bool MarkInstallmentAsPaid(string installmentId);
	Task<PayInstallmentResult> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Receipt>> GetReceiptsAsync(CancellationToken cancellationToken = default);
	Task<Receipt?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default);
	Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default);
	Task<NotificationPreferences> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default);
	Task<NotificationPreferences> UpdateNotificationPreferencesAsync(NotificationPreferences preferences, CancellationToken cancellationToken = default);
}
