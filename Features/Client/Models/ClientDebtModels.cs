#nullable enable
namespace Paynest.Models;

public enum PaymentFrequency
{
	Weekly,
	Biweekly,
	Monthly
}

public enum InstallmentStatus
{
	Pending,
	Paid,
	Overdue
}

public sealed class DebtGroup
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string FreelancerName { get; init; } = string.Empty;
	public decimal TotalAmount { get; init; }
	public decimal PendingAmount { get; init; }
	public PaymentFrequency Frequency { get; init; }
}

public sealed class Installment
{
	public string Id { get; init; } = string.Empty;
	public string GroupId { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public DateTime DueDate { get; init; }
	public decimal Amount { get; init; }
	public InstallmentStatus Status { get; set; }
}

public sealed class Receipt
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

public sealed class PayInstallmentResult
{
	public bool Success { get; init; }
	public decimal? WalletBalance { get; init; }
	public string WalletCurrency { get; init; } = "MXN";
	public string? PaymentId { get; init; }
	public string? PaymentStatus { get; init; }
	public string? PaymentMethod { get; init; }
	public string? ReceiptId { get; init; }
	public string? ReceiptFolio { get; init; }
	public string? ReceiptFileUrl { get; init; }
	public string? MovementId { get; init; }
	public decimal? MovementAmount { get; init; }
}

public sealed class NotificationPreferences
{
	public bool NotifyThreeDaysBefore { get; init; } = true;
	public bool NotifySameDay { get; init; } = true;
	public TimeSpan ReminderTime { get; init; } = new(9, 0, 0);
	public string Timezone { get; init; } = "America/Mexico_City";
	public NotificationChannels Channels { get; init; } = new();
}

public sealed class NotificationChannels
{
	public bool Push { get; init; } = true;
	public bool Email { get; init; }
}

public sealed class CardMovement
{
	public string Description { get; init; } = string.Empty;
	public decimal Amount { get; init; }
	public DateTime CreatedAt { get; init; }
}
