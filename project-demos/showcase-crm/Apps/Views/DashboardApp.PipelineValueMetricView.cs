/*
The total monetary value of all open deals in the pipeline. Represents potential future revenue and sales forecast.
SUM(Deal.Amount) WHERE Deal.Stage indicates open/active status AND Deal.CreatedAt is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class PipelineValueMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "Pipeline Value",
            Icons.PiggyBank,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(PipelineValueMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodDeals = await db.Deals
                    .Where(d => d.CreatedAt >= fromDate && d.CreatedAt <= toDate)
                    .Where(d => d.Stage.DescriptionText == "Open" || d.Stage.DescriptionText == "Active")
                    .ToListAsync(ct);

                var currentPeriodPipelineValue = currentPeriodDeals
                    .Sum(d => (double)(d.Amount ?? 0));

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodDeals = await db.Deals
                    .Where(d => d.CreatedAt >= previousFromDate && d.CreatedAt <= previousToDate)
                    .Where(d => d.Stage.DescriptionText == "Open" || d.Stage.DescriptionText == "Active")
                    .ToListAsync(ct);

                var previousPeriodPipelineValue = previousPeriodDeals
                    .Sum(d => (double)(d.Amount ?? 0));

                if (previousPeriodPipelineValue == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodPipelineValue.ToString("C0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = (currentPeriodPipelineValue - previousPeriodPipelineValue) / previousPeriodPipelineValue;

                var goal = previousPeriodPipelineValue * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodPipelineValue / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodPipelineValue.ToString("C0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("C0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}