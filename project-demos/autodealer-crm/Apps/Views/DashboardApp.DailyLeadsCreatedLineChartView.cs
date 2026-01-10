/*
Tracks the number of leads created each day.
SELECT Date(CreatedAt) AS Date, COUNT(*) AS LeadCount FROM Leads WHERE CreatedAt BETWEEN @StartDate AND @EndDate GROUP BY Date(CreatedAt) ORDER BY Date(CreatedAt)
*/
namespace AutodealerCrm.Apps.Views;

public class DailyLeadsCreatedLineChartView(DateTime startDate, DateTime endDate) : ViewBase
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
                var data = await db.Leads
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                    .GroupBy(l => l.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("d MMM"),
                        LeadCount = g.Count()
                    })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date,
                    [e => e.Sum(f => (double)f.LeadCount)],
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily Leads Created").Height(Size.Units(80));

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