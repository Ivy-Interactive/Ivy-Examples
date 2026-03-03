/*
Win Rate: percentage of closed deals (won + lost) that were won.
Measures sales effectiveness and deal qualification quality.
(COUNT(Deal WHERE Stage = 'Closed Won' AND CloseDate in range) / COUNT(Deal WHERE Stage IN ('Closed Won','Closed Lost') AND CloseDate in range)) * 100
*/
namespace ShowcaseCrm.Apps.Views;

public class LeadConversionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "Win Rate",
            Icons.Trophy,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(LeadConversionRateMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var closedWon = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Won"
                                && d.CloseDate >= fromDate
                                && d.CloseDate <= toDate)
                    .CountAsync(ct);

                var closedLost = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Lost"
                                && d.CloseDate >= fromDate
                                && d.CloseDate <= toDate)
                    .CountAsync(ct);

                var totalClosed = closedWon + closedLost;
                double currentWinRate = totalClosed > 0
                    ? (double)closedWon / totalClosed * 100
                    : 0;

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var prevClosedWon = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Won"
                                && d.CloseDate >= previousFromDate
                                && d.CloseDate <= previousToDate)
                    .CountAsync(ct);

                var prevClosedLost = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Lost"
                                && d.CloseDate >= previousFromDate
                                && d.CloseDate <= previousToDate)
                    .CountAsync(ct);

                var prevTotalClosed = prevClosedWon + prevClosedLost;
                double previousWinRate = prevTotalClosed > 0
                    ? (double)prevClosedWon / prevTotalClosed * 100
                    : 0;

                if (previousWinRate == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentWinRate.ToString("N1") + "%",
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double trend = (currentWinRate - previousWinRate) / previousWinRate;

                var goal = Math.Min(100, previousWinRate * 1.1);
                double? goalAchievement = goal > 0 ? currentWinRate / goal : null;

                return new MetricRecord(
                    MetricFormatted: currentWinRate.ToString("N1") + "%",
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("N1") + "%"
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}