namespace Paynest.Features.Cobrador.Models;

public record ClientSummary(
    string Id,
    string Name,
    string Initials,
    Color  AvatarColor,
    string DateText,
    string Amount,
    string Status,
    Color  StatusBgColor,
    Color  StatusTextColor
);
