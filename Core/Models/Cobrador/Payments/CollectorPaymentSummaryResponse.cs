namespace Paynest.Core.Models.Cobrador.Payments;

public sealed record CollectorPaymentSummaryResponse(
    string Range,
    int PaymentsCount,
    decimal TotalCollectedAmount);

public enum CollectorPaymentSummaryRange
{
    Today,
    Week,
    Month
}
