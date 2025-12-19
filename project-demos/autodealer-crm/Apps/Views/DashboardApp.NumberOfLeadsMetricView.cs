/*
The total number of leads generated within the selected date range.
COUNT(Lead.Id WHERE Lead.CreatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class NumberOfLeadsMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        
        async Task<MetricRecord> CalculateNumberOfLeads()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodLeads = await db.Leads
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                .CountAsync();
                
            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodLeads = await db.Leads
                .Where(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate)
                .CountAsync();

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
        }

        return new MetricView(
            "Number of Leads",
            Icons.Users,
            CalculateNumberOfLeads
        );
    }
}