/*
The percentage of leads that converted into sales within the selected date range.
(COUNT(Vehicle.Id WHERE Vehicle.CreatedAt BETWEEN StartDate AND EndDate) / COUNT(Lead.Id WHERE Lead.CreatedAt BETWEEN StartDate AND EndDate)) * 100
*/
namespace AutodealerCrm.Apps.Views;

public class ConversionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        QueryResult<MetricRecord> CalculateConversionRate(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();
                    
                    var currentPeriodLeadsCount = await db.Leads
                        .Where(l => l.CreatedAt >= fd && l.CreatedAt <= td)
                        .CountAsync(ct);
                        
                    var currentPeriodVehiclesCount = await db.Vehicles
                        .Where(v => v.CreatedAt >= fd && v.CreatedAt <= td)
                        .CountAsync(ct);
                        
                    var currentConversionRate = currentPeriodLeadsCount > 0 
                        ? (double)currentPeriodVehiclesCount / currentPeriodLeadsCount * 100 
                        : 0.0;
                    
                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);
                    
                    var previousPeriodLeadsCount = await db.Leads
                        .Where(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate)
                        .CountAsync(ct);
                        
                    var previousPeriodVehiclesCount = await db.Vehicles
                        .Where(v => v.CreatedAt >= previousFromDate && v.CreatedAt <= previousToDate)
                        .CountAsync(ct);
                    
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
                });
        }

        return new MetricView(
            "Conversion Rate",
            Icons.TrendingUp,
            CalculateConversionRate
        );
    }
}