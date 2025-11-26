/*
The percentage of leads that converted into sales within the selected date range.
(COUNT(Vehicle.Id WHERE Vehicle.CreatedAt BETWEEN StartDate AND EndDate) / COUNT(Lead.Id WHERE Lead.CreatedAt BETWEEN StartDate AND EndDate)) * 100
*/
namespace AutodealerCrm.Apps.Views;

public class ConversionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        
        async Task<MetricRecord> CalculateConversionRate()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodLeadsCount = await db.Leads
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                .CountAsync();
                
            var currentPeriodVehiclesCount = await db.Vehicles
                .Where(v => v.CreatedAt >= fromDate && v.CreatedAt <= toDate)
                .CountAsync();
                
            var currentConversionRate = currentPeriodLeadsCount > 0 
                ? (double)currentPeriodVehiclesCount / currentPeriodLeadsCount * 100 
                : 0.0;
            
            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodLeadsCount = await db.Leads
                .Where(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate)
                .CountAsync();
                
            var previousPeriodVehiclesCount = await db.Vehicles
                .Where(v => v.CreatedAt >= previousFromDate && v.CreatedAt <= previousToDate)
                .CountAsync();
                
            double? previousConversionRate = null;
            if (previousPeriodLeadsCount > 0)
            {
                previousConversionRate = (double)previousPeriodVehiclesCount / previousPeriodLeadsCount * 100;
            }

            if (previousConversionRate == null || previousConversionRate == 0) 
            {
                return new MetricRecord(
                    MetricFormatted: currentConversionRate.ToString("N2") + "%",
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }
            
            double? trend = (currentConversionRate - previousConversionRate.Value) / previousConversionRate.Value;

            var goal = previousConversionRate.Value * 1.1;
            double? goalAchievement = goal > 0 ? (double?)(currentConversionRate / goal ): null;
            
            return new MetricRecord(
                MetricFormatted: currentConversionRate.ToString("N2") + "%",
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N2") + "%"
            );
        }

        return new MetricView(
            "Conversion Rate",
            Icons.TrendingUp,
            CalculateConversionRate
        );
    }
}