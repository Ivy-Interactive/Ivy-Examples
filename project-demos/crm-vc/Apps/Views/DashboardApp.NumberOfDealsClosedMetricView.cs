/*
The total number of deals closed during the selected date range.
COUNT(Deal.Id WHERE Deal.DealDate BETWEEN StartDate AND EndDate)
*/
namespace Vc.Apps.Views;

public class NumberOfDealsClosedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        
        async Task<MetricRecord> CalculateNumberOfDealsClosed()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .CountAsync();
                
            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .CountAsync();

            if (previousPeriodDeals == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentPeriodDeals.ToString("N0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }
            
            double? trend = ((double)currentPeriodDeals - previousPeriodDeals) / previousPeriodDeals;
            
            var goal = previousPeriodDeals * 1.1;
            double? goalAchievement = goal > 0 ? currentPeriodDeals / goal : null;
            
            return new MetricRecord(
                MetricFormatted: currentPeriodDeals.ToString("N0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N0")
            );
        }

        return new MetricView(
            "Number of Deals Closed",
            Icons.Briefcase,
            CalculateNumberOfDealsClosed
        );
    }
}