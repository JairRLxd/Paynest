namespace Paynest.Core.Models.Cobrador.Dashboard;

public sealed record CollectorDashboardResponse(
    decimal CollectedToday,
    decimal DeltaVsYesterdayPercent,
    bool    DeltaIsPositive,
    decimal CollectedThisWeek,
    decimal WeeklyGoal,
    decimal TotalPending,
    int     PendingClientsCount,
    decimal TotalOverdue,
    int     OverdueClientsCount);
