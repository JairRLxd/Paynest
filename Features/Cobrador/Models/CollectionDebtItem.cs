namespace Paynest.Features.Cobrador.Models;

public record CollectionDebtItem(
    string Initials,
    Color  AvatarColor,
    string ClientId,
    string DebtId,
    string ClientName,
    string Description,
    DateTime DueDate,
    string Amount,           // Monto base (RemainingAmount)
    string Status,
    Color  StatusBg,
    Color  StatusText,
    string DueText,
    bool   IsOverdueItem      = false,
    bool   IsDueTodayItem     = false,
    bool   IsInThisWeekItem   = false,
    bool   HasInterest        = false,
    string InterestLabel      = "",
    string InterestAmount     = "",
    string TotalAmount        = "",
    bool   HasPartialPayment  = false)
{
    public string DisplayAmount => HasInterest && !string.IsNullOrEmpty(TotalAmount) ? TotalAmount : Amount;
    public bool   CanEdit       => !HasPartialPayment;
}
