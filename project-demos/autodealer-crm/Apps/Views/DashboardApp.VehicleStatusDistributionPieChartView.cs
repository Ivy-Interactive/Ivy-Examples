/*
Shows the distribution of vehicles by their status.
SELECT VehicleStatus.DescriptionText AS Status, COUNT(*) AS VehicleCount FROM Vehicles INNER JOIN VehicleStatuses ON Vehicles.VehicleStatusId = VehicleStatuses.Id WHERE Vehicles.CreatedAt BETWEEN @StartDate AND @EndDate GROUP BY VehicleStatus.DescriptionText
*/
namespace AutodealerCrm.Apps.Views;

public class VehicleStatusDistributionPieChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var chart = UseState<object?>((object?)null!);
        var exception = UseState<Exception?>((Exception?)null!);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                var data = await db.Vehicles
                    .Where(v => v.CreatedAt >= startDate && v.CreatedAt <= endDate)
                    .GroupBy(v => v.VehicleStatus.DescriptionText)
                    .Select(g => new
                    {
                        Status = g.Key,
                        VehicleCount = g.Count()
                    })
                    .ToListAsync();

                var totalVehicles = data.Sum(d => d.VehicleCount);

                PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalVehicles), "Vehicles");

                chart.Set(data.ToPieChart(
                    dimension: e => e.Status,
                    measure: e => e.Sum(f => f.VehicleCount),
                    PieChartStyles.Dashboard,
                    total));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Vehicle Status Distribution").Height(Size.Units(80));

        if (exception.Value != null)
        {
            return card | new ErrorTeaserView(exception.Value);
        }

        if (chart.Value == null)
        {
            return card | new Skeleton();
        }

        return card | chart.Value;
    }
}