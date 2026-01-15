/*
The total revenue generated from vehicle sales within the selected date range.
SUM(Vehicle.Price WHERE Vehicle.CreatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class TotalSalesRevenueMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        QueryResult<MetricRecord> CalculateTotalSalesRevenue(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();
                    
                    var currentPeriodRevenue = await db.Vehicles
                        .Where(v => v.CreatedAt >= fd && v.CreatedAt <= td)
                        .SumAsync(v => (double)v.Price, ct);
                        
                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);
                    
                    var previousPeriodRevenue = await db.Vehicles
                        .Where(v => v.CreatedAt >= previousFromDate && v.CreatedAt <= previousToDate)
                        .SumAsync(v => (double)v.Price, ct);

                    if (previousPeriodRevenue == 0)
                    {
                        return new MetricRecord(
                            MetricFormatted: currentPeriodRevenue.ToString("C0"),
                            TrendComparedToPreviousPeriod: null,
                            GoalAchieved: null,
                            GoalFormatted: null
                        );
                    }
                    
                    double? trend = (currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue;
                    var goal = previousPeriodRevenue * 1.1;
                    double? goalAchievement = goal > 0 ? (double?)(currentPeriodRevenue / goal ): null;
                    
                    return new MetricRecord(
                        MetricFormatted: currentPeriodRevenue.ToString("C0"),
                        TrendComparedToPreviousPeriod: trend,
                        GoalAchieved: goalAchievement,
                        GoalFormatted: goal.ToString("C0")
                    );
                });
        }

        return new MetricView(
            "Total Sales Revenue",
            Icons.DollarSign,
            CalculateTotalSalesRevenue
        );
    }
}