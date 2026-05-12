namespace Paynest.Features.Cobrador.Models;

public sealed record AgendaCollectionItem(
    string ClientId,
    string DebtId,
    string InstallmentId,
    int InstallmentNumber,
    string ClientName,
    string ClientInitials,
    Color AvatarColor,
    string Address,
    string Description,
    string InstallmentLabel,
    string AmountDueText,
    string StatusLabel,
    Color StatusBackground,
    Color StatusTextColor,
    Color AccentColor,
    bool IsOverdue,
    bool IsRescheduled = false,
    string? RescheduleReason = null,
    DateTime DueDate = default)
{
    public bool HasRescheduleReason => !string.IsNullOrWhiteSpace(RescheduleReason);
}
