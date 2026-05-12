namespace Paynest.Core.Models.Cobrador.Clients.RegisterPayment;

public sealed record RegisterPaymentResponse(
    string   PaymentId,
    string   ClientId,
    decimal  Amount,
    string   Method,
    bool     IsTotalPayment,
    DateTime PaidAt,
    string?  Notes,
    string?  TicketUrl);
