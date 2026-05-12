namespace Paynest.Features.Cobrador.Models;

public sealed record CollectionRecentPaymentItem(
    string Initials,
    Color AvatarColor,
    string ClientName,
    string AmountText,
    string MethodLabel,
    string RegisteredAtText,
    string NotesText);
