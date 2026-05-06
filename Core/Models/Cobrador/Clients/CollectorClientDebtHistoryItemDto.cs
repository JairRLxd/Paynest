namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record CollectorClientDebtHistoryItemDto(
    string  ItemId,
    string  ItemType,       // "payment" | "debt"
    string  Description,
    string  DateDisplay,
    decimal Amount,
    string  Status,         // "Pagado" | "En revisión" | "Vencido" | "Pendiente"
    bool    NeedsReview,
    string? ProofImageUrl);
