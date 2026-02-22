/*
The total number of new leads created during the selected period. Indicates the effectiveness of marketing and lead generation efforts.
COUNT(Lead) WHERE Lead.CreatedAt is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class NewLeadsGeneratedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        return new MetricView(
            "New Leads Generated",
            Icons.UserPlus,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(NewLeadsGeneratedMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodLeads = await db.Leads
                    .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                    .CountAsync(ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodLeads = await db.Leads
                    .Where(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate)
                    .CountAsync(ct);

                if (previousPeriodLeads == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodLeads.ToString("N0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = ((double)currentPeriodLeads - previousPeriodLeads) / previousPeriodLeads;

                var goal = previousPeriodLeads * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodLeads / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodLeads.ToString("N0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("N0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}