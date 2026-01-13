/*
The total number of tasks marked as completed within the selected date range.
COUNT(Task.Id WHERE Task.Completed = 1 AND Task.UpdatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class NumberOfTasksCompletedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        QueryResult<MetricRecord> CalculateNumberOfTasksCompleted(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();
                    
                    // Count completed tasks where UpdatedAt is in the date range (UpdatedAt is when task was marked completed)
                    var currentPeriodTasksCompleted = await db.Tasks
                        .Where(t => t.Completed && t.DueDate >= fd && t.DueDate <= td)
                        .CountAsync(ct);
                    
                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);
                    
                    var previousPeriodTasksCompleted = await db.Tasks
                        .Where(t => t.Completed && t.DueDate >= previousFromDate && t.DueDate <= previousToDate)
                        .CountAsync(ct);

                    if (previousPeriodTasksCompleted == 0)
                    {
                        return new MetricRecord(
                            MetricFormatted: currentPeriodTasksCompleted.ToString("N0"),
                            TrendComparedToPreviousPeriod: null,
                            GoalAchieved: null,
                            GoalFormatted: null
                        );
                    }
                    
                    double? trend = ((double)currentPeriodTasksCompleted - previousPeriodTasksCompleted) / previousPeriodTasksCompleted;
                    
                    var goal = previousPeriodTasksCompleted * 1.1;
                    double? goalAchievement = goal > 0 ? (double?)(currentPeriodTasksCompleted / goal ): null;
                    
                    return new MetricRecord(
                        MetricFormatted: currentPeriodTasksCompleted.ToString("N0"),
                        TrendComparedToPreviousPeriod: trend,
                        GoalAchieved: goalAchievement,
                        GoalFormatted: goal.ToString("N0")
                    );
                });
        }

        return new MetricView(
            "Number of Tasks Completed",
            Icons.ListCheck,
            CalculateNumberOfTasksCompleted
        );
    }
}