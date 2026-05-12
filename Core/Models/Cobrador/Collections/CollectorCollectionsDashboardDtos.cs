namespace Paynest.Core.Models.Cobrador.Collections;

public sealed record CollectorCollectionsDashboardResponse(
    CollectorCollectionsPortfolioDto Portfolio,
    CollectorCollectionsRangeSummaryDto Today,
    CollectorCollectionsRangeSummaryDto Week,
    CollectorCollectionsRangeSummaryDto? CollectedToday,
    CollectorCollectionsRangeSummaryDto? CollectedWeek);

public sealed record CollectorCollectionsPortfolioDto(
    decimal TotalOutstandingAmount,
    int TotalActiveDebtsCount,
    int OverdueInstallmentsCount,
    decimal TotalOverdueAmount);

public sealed record CollectorCollectionsRangeSummaryDto(
    int ItemsCount,
    decimal TotalAmount);
