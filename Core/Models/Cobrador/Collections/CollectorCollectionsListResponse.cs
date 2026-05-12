namespace Paynest.Core.Models.Cobrador.Collections;

public sealed record CollectorCollectionsListResponse(
    string Filter,
    CollectorCollectionsListSummaryDto Summary,
    IReadOnlyList<CollectorCollectionsSectionDto> Sections);

public sealed record CollectorCollectionsListSummaryDto(
    int ItemsCount,
    decimal TotalAmount);

public sealed record CollectorCollectionsSectionDto(
    string Key,
    string Title,
    string Subtitle,
    int ItemsCount,
    IReadOnlyList<CollectorCollectionItemDto> Items);
