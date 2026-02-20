/*
The average monetary value of closed deals. Helps understand deal quality and pricing effectiveness.
AVG(Deal.Amount) WHERE Deal.Stage indicates closed/won status AND Deal.CloseDate is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class AverageDealSizeMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "Average Deal Size",
            Icons.TrendingUp,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(AverageDealSizeMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodDeals = await db.Deals
                    .Where(d => d.CloseDate >= fromDate && d.CloseDate <= toDate)
                    .Where(d => d.Stage.DescriptionText == "Closed/Won")
                    .ToListAsync(ct);

                var currentAverageDealSize = currentPeriodDeals.Any()
                    ? currentPeriodDeals.Average(d => (double)(d.Amount ?? 0))
                    : 0.0;

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodDeals = await db.Deals
                    .Where(d => d.CloseDate >= previousFromDate && d.CloseDate <= previousToDate)
                    .Where(d => d.Stage.DescriptionText == "Closed/Won")
                    .ToListAsync(ct);

                var previousAverageDealSize = previousPeriodDeals.Any()
                    ? previousPeriodDeals.Average(d => (double)(d.Amount ?? 0))
                    : 0.0;

                if (previousAverageDealSize == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentAverageDealSize.ToString("C2"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = (currentAverageDealSize - previousAverageDealSize) / previousAverageDealSize;

                var goal = previousAverageDealSize * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentAverageDealSize / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentAverageDealSize.ToString("C2"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("C2")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}