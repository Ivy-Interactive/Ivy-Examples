/*
Shows the number of startups founded each day within the selected date range.
SELECT FoundedAt AS Date, COUNT(Id) AS StartupCount FROM Startups WHERE FoundedAt BETWEEN @StartDate AND @EndDate GROUP BY FoundedAt ORDER BY FoundedAt
*/
namespace Vc.Apps.Views;

public class DailyNewStartupsLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object Build()
    {
        var factory = UseService<VcContextFactory>();
        var chart = UseState<object?>((object?)null!);
        var exception = UseState<Exception?>((Exception?)null!);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                var data = await db.Startups
                    .Where(s => s.FoundedAt >= startDate && s.FoundedAt <= endDate)
                    .GroupBy(s => s.FoundedAt!.Value.Date)
                    .Select(g => new StartupData { Date = g.Key.ToString("d MMM"), StartupCount = g.Count() })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date, 
                    [e => e.Sum(f => f.StartupCount)], 
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily New Startups").Height(Size.Units(80));

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

    private class StartupData
    {
        public string Date { get; set; }
        public int StartupCount { get; set; }
    }
}