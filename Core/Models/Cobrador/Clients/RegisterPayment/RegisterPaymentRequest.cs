namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record RegisterPaymentRequest(
    decimal   Amount,
    bool      IsTotalPayment,
    string    Method,           // "Efectivo" | "Transferencia" | "Tarjeta" | "Otro"
    DateTime? PaymentDateTime,
    string?   Notes,
    string?   ProofFilePath);
