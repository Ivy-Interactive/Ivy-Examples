/*
The average monetary value of deals closed during the selected date range.
AVG(Deal.Amount WHERE Deal.DealDate BETWEEN StartDate AND EndDate)
*/
namespace Vc.Apps.Views;

public class AverageDealSizeMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        
        async Task<MetricRecord> CalculateAverageDealSize()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .ToListAsync();
                
            var currentAverageDealSize = currentPeriodDeals.Any()
                ? currentPeriodDeals.Average(d => (double)d.Amount.GetValueOrDefault())
                : 0;

            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .ToListAsync();
                
            var previousAverageDealSize = previousPeriodDeals.Any()
                ? previousPeriodDeals.Average(d => (double)d.Amount.GetValueOrDefault())
                : 0;

            if (previousAverageDealSize == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentAverageDealSize.ToString("C0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }
            
            double? trend = (currentAverageDealSize - previousAverageDealSize) / previousAverageDealSize;
            var goal = previousAverageDealSize * 1.1;
            double? goalAchievement = goal > 0 ? currentAverageDealSize / goal : null;
            
            return new MetricRecord(
                MetricFormatted: currentAverageDealSize.ToString("C0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("C0")
            );
        }

        return new MetricView(
            "Average Deal Size",
            null,
            CalculateAverageDealSize
        );
    }
}