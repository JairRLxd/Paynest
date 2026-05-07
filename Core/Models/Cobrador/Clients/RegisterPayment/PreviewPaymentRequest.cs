namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record PreviewPaymentRequest(
    decimal?  Amount,
    bool      IsTotalPayment,
    string    Method,
    DateTime? PaymentDateTime,
    string?   Notes = null);
