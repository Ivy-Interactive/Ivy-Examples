/*
The average time taken to respond to leads within the selected date range.
AVG(DATEDIFF(Message.SentAt, Lead.CreatedAt) WHERE Message.LeadId = Lead.Id AND Lead.CreatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class AverageLeadResponseTimeMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        QueryResult<MetricRecord> CalculateAverageLeadResponseTime(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();

                    // Filter messages where SentAt is after Lead creation (no negative response times)
                    var currentPeriodMessages = await db.Messages
                        .Where(m => m.LeadId != null && 
                                   m.Lead!.CreatedAt >= fd && 
                                   m.Lead.CreatedAt <= td &&
                                   m.SentAt >= m.Lead!.CreatedAt)
                        .Include(m => m.Lead)
                        .ToListAsync(ct);

                    // SentAt is now DateTime, so we can use it directly
                    var currentPeriodAverageResponseTime = currentPeriodMessages.Any()
                        ? currentPeriodMessages.Average(m => (m.SentAt - m.Lead!.CreatedAt).TotalMinutes)
                        : 0.0;

                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);

                    var previousPeriodMessages = await db.Messages
                        .Where(m => m.LeadId != null && 
                                   m.Lead!.CreatedAt >= previousFromDate && 
                                   m.Lead.CreatedAt <= previousToDate &&
                                   m.SentAt >= m.Lead!.CreatedAt)
                        .Include(m => m.Lead)
                        .ToListAsync(ct);

                    var previousPeriodAverageResponseTime = previousPeriodMessages.Any()
                        ? previousPeriodMessages.Average(m => (m.SentAt - m.Lead!.CreatedAt).TotalMinutes)
                        : 0.0;

                    if (previousPeriodAverageResponseTime == 0)
                    {
                        return new MetricRecord(
                            MetricFormatted: currentPeriodAverageResponseTime.ToString("N2") + " mins",
                            TrendComparedToPreviousPeriod: null,
                            GoalAchieved: null,
                            GoalFormatted: null
                        );
                    }

                    // For response time: lower is better, so trend is inverted
                    double? trend = (previousPeriodAverageResponseTime - currentPeriodAverageResponseTime) / previousPeriodAverageResponseTime;
                    
                    // Goal is 10% improvement (10% less time = faster response)
                    var goal = previousPeriodAverageResponseTime * 0.9;
                    // GoalAchieved: if current is less than goal (better), achievement > 1
                    double? goalAchievement = currentPeriodAverageResponseTime > 0 ? (double?)(goal / currentPeriodAverageResponseTime) : null;

                    return new MetricRecord(
                        MetricFormatted: currentPeriodAverageResponseTime.ToString("N2") + " mins",
                        TrendComparedToPreviousPeriod: trend,
                        GoalAchieved: goalAchievement,
                        GoalFormatted: goal.ToString("N2") + " mins"
                    );
                });
        }

        return new MetricView(
            "Average Lead Response Time",
            Icons.Clock,
            CalculateAverageLeadResponseTime
        );
    }
}