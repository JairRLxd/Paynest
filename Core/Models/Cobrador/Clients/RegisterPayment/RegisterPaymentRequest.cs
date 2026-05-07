namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record RegisterPaymentRequest(
    decimal   Amount,
    bool      IsTotalPayment,
    string    Method,           // "Cash" | "Transfer" | "Card"
    DateTime? PaymentDateTime,
    string?   Notes,
    string?   ProofFilePath);
