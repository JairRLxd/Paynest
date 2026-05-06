namespace Paynest.Core.Models.Cobrador.Clients;

public sealed record CollectorClientListResponse(
    IReadOnlyList<CollectorClientSummaryDto> Items,
    int TotalCount);
