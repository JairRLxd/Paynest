#nullable enable
namespace Paynest.Features.Client.Api;

public interface IDebtApiClient
{
	Task<IReadOnlyList<DebtGroupDto>> GetDebtGroupsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<InstallmentDto>> GetInstallmentsByGroupAsync(string groupId, CancellationToken cancellationToken = default);
	Task<PayInstallmentResponseDto> MarkInstallmentAsPaidAsync(string installmentId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ReceiptDto>> GetReceiptsAsync(CancellationToken cancellationToken = default);
	Task<ReceiptDto?> GetReceiptAsync(string receiptId, CancellationToken cancellationToken = default);
	Task<string?> GetReceiptDownloadUrlAsync(string receiptId, CancellationToken cancellationToken = default);
	Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(CancellationToken cancellationToken = default);
	Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(NotificationPreferencesDto request, CancellationToken cancellationToken = default);
	Task<WalletDto> GetWalletAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<WalletMovementDto>> GetWalletMovementsAsync(int limit = 20, CancellationToken cancellationToken = default);
	Task<WalletDepositResponseDto> DepositWalletAsync(WalletDepositRequestDto request, CancellationToken cancellationToken = default);
	Task<LinkCollectorResponseDto> LinkCollectorAsync(LinkCollectorRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class DebtGroupDto
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string FreelancerName { get; init; } = string.Empty;
	public decimal TotalAmount { get; init; }
	public decimal PendingAmount { get; init; }
	public string Frequency { get; init; } = string.Empty;
}

public sealed class InstallmentDto
{
	public string Id { get; init; } = string.Empty;
	public string GroupId { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public DateTime DueDate { get; init; }
	public decimal Amount { get; init; }
	public string Status { get; init; } = string.Empty;
}

public sealed class ReceiptDto
{
	public string Id { get; init; } = string.Empty;
	public string InstallmentId { get; init; } = string.Empty;
	public string GroupId { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public decimal Amount { get; init; }
	public DateTime PaidAt { get; init; }
	public string Folio { get; init; } = string.Empty;
	public string? FileUrl { get; init; }
}

public sealed class PayInstallmentRequestDto
{
	public string Source { get; init; } = "wallet";
}

public sealed class PayInstallmentResponseDto
{
	public bool Success { get; init; }
	public WalletDto? Wallet { get; init; }
	public PaymentDto? Payment { get; init; }
	public ReceiptDto? Receipt { get; init; }
	public WalletMovementDto? Movement { get; init; }
}

public sealed class PaymentDto
{
	public string Id { get; init; } = string.Empty;
	public string InstallmentId { get; init; } = string.Empty;
	public decimal Amount { get; init; }
	public string Currency { get; init; } = "MXN";
	public string Method { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public string Reference { get; init; } = string.Empty;
	public DateTime? PaidAt { get; init; }
}

public sealed class NotificationPreferencesDto
{
	public bool NotifyThreeDaysBefore { get; init; }
	public bool NotifySameDay { get; init; }
	public string ReminderTime { get; init; } = "09:00";
	public string Timezone { get; init; } = "America/Mexico_City";
	public NotificationChannelsDto Channels { get; init; } = new();
}

public sealed class NotificationChannelsDto
{
	public bool Push { get; init; } = true;
	public bool Email { get; init; }
}

public sealed class WalletDto
{
	public string Id { get; init; } = string.Empty;
	public decimal Balance { get; init; }
	public string Currency { get; init; } = "MXN";
	public string Status { get; init; } = "active";
}

public sealed class WalletMovementDto
{
	public string Id { get; init; } = string.Empty;
	public string Type { get; init; } = string.Empty;
	public decimal Amount { get; init; }
	public string Currency { get; init; } = "MXN";
	public string Description { get; init; } = string.Empty;
	public string Reference { get; init; } = string.Empty;
	public string? RelatedInstallmentId { get; init; }
	public string? RelatedPaymentId { get; init; }
	public DateTime CreatedAt { get; init; }
}

public sealed class WalletDepositRequestDto
{
	public decimal Amount { get; init; }
	public string Description { get; init; } = string.Empty;
}

public sealed class WalletDepositResponseDto
{
	public WalletDto Wallet { get; init; } = new();
	public WalletMovementDto Movement { get; init; } = new();
}

public sealed class LinkCollectorRequestDto
{
	public string CollectorCode { get; init; } = string.Empty;
}

public sealed class LinkCollectorResponseDto
{
	public string CollectorId { get; init; } = string.Empty;
	public string CollectorName { get; init; } = string.Empty;
	public string RelationshipId { get; init; } = string.Empty;
	public DateTime LinkedAt { get; init; }
}
