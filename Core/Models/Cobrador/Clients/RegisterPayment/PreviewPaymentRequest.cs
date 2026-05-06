namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record PreviewPaymentRequest(
    decimal   Amount,
    bool      IsTotalPayment,
    DateTime? PaymentDateTime);
