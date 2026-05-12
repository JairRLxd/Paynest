namespace Paynest.Core.Models.Cobrador.Payments;

public sealed record CollectorRecentPaymentsResponse(
    IReadOnlyList<CollectorRecentPaymentItemDto> Items);

public sealed record CollectorRecentPaymentItemDto(
    string PaymentId,
    string ClientId,
    string ClientName,
    decimal Amount,
    string Method,
    DateTime RegisteredAt,
    string? Notes);
