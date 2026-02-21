/*
The total amount of revenue generated from all closed deals. This is the North Star metric that directly measures the company's financial success and growth.
SUM(Deal.Amount) WHERE Deal.Stage indicates closed/won status AND Deal.CloseDate is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class TotalRevenueMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        return new MetricView(
            "Total Revenue",
            Icons.DollarSign,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(TotalRevenueMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodRevenue = await db.Deals
                    .Where(d => d.CloseDate >= fromDate && d.CloseDate <= toDate)
                    .Where(d => d.Stage.DescriptionText == "Closed Won")
                    .SumAsync(d => (double)(d.Amount ?? 0), ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodRevenue = await db.Deals
                    .Where(d => d.CloseDate >= previousFromDate && d.CloseDate <= previousToDate)
                    .Where(d => d.Stage.DescriptionText == "Closed Won")
                    .SumAsync(d => (double)(d.Amount ?? 0), ct);

                if (previousPeriodRevenue == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodRevenue.ToString("C0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = (currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue;

                var goal = previousPeriodRevenue * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodRevenue / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodRevenue.ToString("C0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("C0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}