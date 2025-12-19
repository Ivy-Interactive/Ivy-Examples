/*
The total number of messages sent to customers within the selected date range.
COUNT(Message.Id WHERE Message.MessageDirectionId = (SELECT Id FROM MessageDirection WHERE DescriptionText = 'Outbound') AND Message.SentAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class TotalMessagesSentMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        
        async Task<MetricRecord> CalculateTotalMessagesSent()
        {
            await using var db = factory.CreateDbContext();
            
            var outboundDirectionId = await db.MessageDirections
                .Where(md => md.DescriptionText == "Outgoing")
                .Select(md => md.Id)
                .FirstOrDefaultAsync();

            // SentAt is now DateTime, so we can compare directly
            var currentPeriodMessagesSent = await db.Messages
                .Where(m => m.MessageDirectionId == outboundDirectionId && 
                            m.SentAt >= fromDate && m.SentAt <= toDate)
                .CountAsync();

            var previousPeriodMessagesSent = await db.Messages
                .Where(m => m.SentAt >= fromDate && m.SentAt <= toDate)
                .CountAsync();

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
        }

        return new MetricView(
            "Total Messages Sent",
            Icons.MessageCircle,
            CalculateTotalMessagesSent
        );
    }
}