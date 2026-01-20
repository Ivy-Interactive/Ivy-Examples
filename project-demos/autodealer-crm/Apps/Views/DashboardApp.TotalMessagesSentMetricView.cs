/*
The total number of messages sent to customers within the selected date range.
COUNT(Message.Id WHERE Message.MessageDirectionId = (SELECT Id FROM MessageDirection WHERE DescriptionText = 'Outbound') AND Message.SentAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class TotalMessagesSentMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        QueryResult<MetricRecord> CalculateTotalMessagesSent(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();
                    
                    var outboundDirectionId = await db.MessageDirections
                        .Where(md => md.DescriptionText == "Outgoing")
                        .Select(md => md.Id)
                        .FirstOrDefaultAsync(ct);

                    // SentAt is now DateTime, so we can compare directly
                    var currentPeriodMessagesSent = await db.Messages
                        .Where(m => m.MessageDirectionId == outboundDirectionId && 
                                    m.SentAt >= fd && m.SentAt <= td)
                        .CountAsync(ct);

                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);

                    var previousPeriodMessagesSent = await db.Messages
                        .Where(m => m.MessageDirectionId == outboundDirectionId && 
                                    m.SentAt >= previousFromDate && m.SentAt <= previousToDate)
                        .CountAsync(ct);

                    if (previousPeriodMessagesSent == 0)
                    {
                        return new MetricRecord(
                            MetricFormatted: currentPeriodMessagesSent.ToString("N0"),
                            TrendComparedToPreviousPeriod: null,
                            GoalAchieved: null,
                            GoalFormatted: null
                        );
                    }

                    double? trend = ((double)currentPeriodMessagesSent - previousPeriodMessagesSent) / previousPeriodMessagesSent;

                    var goal = previousPeriodMessagesSent * 1.1;
                    double? goalAchievement = goal > 0 ? (double?)(currentPeriodMessagesSent / goal ): null;

                    return new MetricRecord(
                        MetricFormatted: currentPeriodMessagesSent.ToString("N0"),
                        TrendComparedToPreviousPeriod: trend,
                        GoalAchieved: goalAchievement,
                        GoalFormatted: goal.ToString("N0")
                    );
                });
        }

        return new MetricView(
            "Total Messages Sent",
            Icons.MessageCircle,
            CalculateTotalMessagesSent
        );
    }
}