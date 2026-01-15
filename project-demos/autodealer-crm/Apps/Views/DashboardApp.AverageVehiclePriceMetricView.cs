/*
The average price of vehicles listed within the selected date range.
AVG(Vehicle.Price WHERE Vehicle.CreatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class AverageVehiclePriceMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        QueryResult<MetricRecord> CalculateAverageVehiclePrice(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();

                    var currentPeriodVehicles = await db.Vehicles
                        .Where(v => v.CreatedAt >= fd && v.CreatedAt <= td)
                        .ToListAsync(ct);

                    var currentAveragePrice = currentPeriodVehicles.Any()
                        ? currentPeriodVehicles.Average(v => (double)v.Price)
                        : 0.0;

                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);

                    var previousPeriodVehicles = await db.Vehicles
                        .Where(v => v.CreatedAt >= previousFromDate && v.CreatedAt <= previousToDate)
                        .ToListAsync(ct);

                    var previousAveragePrice = previousPeriodVehicles.Any()
                        ? previousPeriodVehicles.Average(v => (double)v.Price)
                        : 0.0;

                    if (previousAveragePrice == 0.0)
                    {
                        return new MetricRecord(
                            MetricFormatted: currentAveragePrice.ToString("C0"),
                            TrendComparedToPreviousPeriod: null,
                            GoalAchieved: null,
                            GoalFormatted: null
                        );
                    }

                    double? trend = (currentAveragePrice - previousAveragePrice) / previousAveragePrice;

                    var goal = previousAveragePrice * 1.1;
                    double? goalAchievement = goal > 0 ? (double?)(currentAveragePrice / goal ): null;

                    return new MetricRecord(
                        MetricFormatted: currentAveragePrice.ToString("C0"),
                        TrendComparedToPreviousPeriod: trend,
                        GoalAchieved: goalAchievement,
                        GoalFormatted: goal.ToString("C0")
                    );
                });
        }

        return new MetricView(
            "Average Vehicle Price",
            Icons.Car,
            CalculateAverageVehiclePrice
        );
    }
}