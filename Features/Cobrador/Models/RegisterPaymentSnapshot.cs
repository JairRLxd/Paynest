namespace Paynest.Features.Cobrador.Models;

public sealed record RegisterPaymentSnapshot(
    string ClientId,
    string ClientName,
    string ClientNameUpper,
    string StatusLabel,
    Color  StatusColor,
    string DebtId = "",
    string InstallmentId = "");
