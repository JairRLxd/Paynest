namespace Paynest.Features.Cobrador.Clients.Models;

public sealed record RegisterPaymentSnapshot(
    string ClientId,
    string ClientName,
    string ClientNameUpper,
    string StatusLabel,
    Color  StatusColor,
    string DebtId = "");
