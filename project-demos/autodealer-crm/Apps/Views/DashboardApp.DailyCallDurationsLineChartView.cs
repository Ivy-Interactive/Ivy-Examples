/*
Displays the total duration of calls made each day.
SELECT Date(StartTime) AS Date, SUM(Duration) AS TotalDuration FROM CallRecords WHERE StartTime BETWEEN @StartDate AND @EndDate GROUP BY Date(StartTime) ORDER BY Date(StartTime)
*/
namespace AutodealerCrm.Apps.Views;

public class DailyCallDurationsLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var chart = UseState<object?>((object?)null);
        var exception = UseState<Exception?>((Exception?)null);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                
                // StartTime is now DateTime, so we can compare directly and group in SQL
                var data = await db.CallRecords
                    .Where(cr => cr.StartTime >= startDate && cr.StartTime <= endDate)
                    .GroupBy(cr => cr.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("d MMM"),
                        TotalDuration = g.Sum(cr => (double)(cr.Duration ?? 0))
                    })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date,
                    [e => e.Sum(f => f.TotalDuration)],
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily Call Durations").Height(Size.Units(80));

        if (exception.Value != null)
        {
            return card | new ErrorTeaserView(exception.Value);
        }

        // if (chart.Value == null)
        // {
        //     return card | new Skeleton();
        // }

        return card | chart.Value;
    }
}