namespace Paynest.Core.Models.Cobrador.Collections;

public sealed record CollectorCollectionsResponse(
    decimal TotalOutstandingAmount,
    int     TotalActiveDebtsCount,
    int     OverdueInstallmentsCount,
    decimal TotalOverdueAmount,
    IReadOnlyList<CollectorCollectionItemDto> Items);
