namespace Paynest.Features.Cobrador.Collections.Models;

public record CollectionDebtItem(
    string Initials,
    Color  AvatarColor,
    string ClientId,
    string DebtId,
    string ClientName,
    string Description,
    string Amount,           // Monto base (RemainingAmount)
    string Status,
    Color  StatusBg,
    Color  StatusText,
    string DueText,
    bool   HasInterest    = false,
    string InterestLabel  = "",  // e.g. "Mora (10%)"
    string InterestAmount = "",  // e.g. "+ $120.00"
    string TotalAmount    = "")  // e.g. "$1,320.00"
{
    public string DisplayAmount
        => HasInterest && !string.IsNullOrEmpty(TotalAmount) ? TotalAmount : Amount;
}
