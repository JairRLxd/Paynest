namespace Paynest.Features.Cobrador.Models;

public sealed record PaymentDebtSummaryItem(
    string Title,
    string Subtitle,
    string PrincipalAmount,
    string MoratoryAmount,
    string TotalAmount,
    string StatusLabel,
    Color StatusBackground,
    Color StatusTextColor)
{
    public bool HasMoratoryAmount => !string.IsNullOrWhiteSpace(MoratoryAmount) && MoratoryAmount != "$0.00";
}
