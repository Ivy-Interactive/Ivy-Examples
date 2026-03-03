/*
The total monetary value of all open deals in the pipeline (Prospecting, Qualification, Proposal).
Represents potential future revenue.
*/
namespace ShowcaseCrm.Apps.Views;

public class PipelineValueMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    private static readonly string[] OpenStages = ["Prospecting", "Qualification", "Proposal"];

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

                var currentPeriodPipelineValue = await db.Deals
                    .Where(d => OpenStages.Contains(d.Stage.DescriptionText) && d.Amount.HasValue)
                    .SumAsync(d => (double)d.Amount!.Value, ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodPipelineValue = await db.Deals
                    .Where(d => d.CreatedAt >= previousFromDate && d.CreatedAt <= previousToDate)
                    .Where(d => OpenStages.Contains(d.Stage.DescriptionText) && d.Amount.HasValue)
                    .SumAsync(d => (double)d.Amount!.Value, ct);

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
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodPipelineValue / goal) : null;

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
