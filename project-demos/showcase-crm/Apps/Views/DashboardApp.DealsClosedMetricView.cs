/*
The total number of deals that were successfully closed during the selected period. Measures sales team performance and conversion effectiveness.
COUNT(Deal) WHERE Deal.Stage indicates closed/won status AND Deal.CloseDate is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class DealsClosedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "Deals Closed",
            null,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(DealsClosedMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodDealsClosed = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Won" && d.CloseDate >= fromDate && d.CloseDate <= toDate)
                    .CountAsync(ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodDealsClosed = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Won" && d.CloseDate >= previousFromDate && d.CloseDate <= previousToDate)
                    .CountAsync(ct);

                if (previousPeriodDealsClosed == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodDealsClosed.ToString("N0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = ((double)currentPeriodDealsClosed - previousPeriodDealsClosed) / previousPeriodDealsClosed;

                var goal = previousPeriodDealsClosed * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodDealsClosed / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodDealsClosed.ToString("N0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("N0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}