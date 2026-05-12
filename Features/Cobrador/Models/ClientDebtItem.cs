namespace Paynest.Features.Cobrador.Models;

public record ClientDebtItem(
    string  Title,
    string  DateText,
    string  Amount,
    string  Status,
    string  IconGlyph,
    Color   IconBackground,
    Color   IconColor,
    Color   StatusBackground,
    Color   StatusTextColor,
    bool    NeedsReview    = false,
    string? ProofImagePath = null,
    string? PaymentId      = null,
    string? ClientId       = null);
