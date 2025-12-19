/*
The average price of vehicles listed within the selected date range.
AVG(Vehicle.Price WHERE Vehicle.CreatedAt BETWEEN StartDate AND EndDate)
*/
namespace AutodealerCrm.Apps.Views;

public class AverageVehiclePriceMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();

        async Task<MetricRecord> CalculateAverageVehiclePrice()
        {
            await using var db = factory.CreateDbContext();

            var currentPeriodVehicles = await db.Vehicles
                .Where(v => v.CreatedAt >= fromDate && v.CreatedAt <= toDate)
                .ToListAsync();

            var currentAveragePrice = currentPeriodVehicles.Any()
                ? currentPeriodVehicles.Average(v => (double)v.Price)
                : 0.0;

            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);

            var previousPeriodVehicles = await db.Vehicles
                .Where(v => v.CreatedAt >= previousFromDate && v.CreatedAt <= previousToDate)
                .ToListAsync();

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
        }

        return new MetricView(
            "Average Vehicle Price",
            Icons.Car,
            CalculateAverageVehiclePrice
        );
    }
}