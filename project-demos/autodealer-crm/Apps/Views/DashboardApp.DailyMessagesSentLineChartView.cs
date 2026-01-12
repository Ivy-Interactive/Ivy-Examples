/*
Monitors the number of messages sent daily.
SELECT Date(SentAt) AS Date, COUNT(*) AS MessageCount FROM Messages WHERE SentAt BETWEEN @StartDate AND @EndDate GROUP BY Date(SentAt) ORDER BY Date(SentAt)
*/
namespace AutodealerCrm.Apps.Views;

public class DailyMessagesSentLineChartView(DateTime startDate, DateTime endDate) : ViewBase
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

                // SentAt is now DateTime, so we can compare directly and group in SQL
                var data = await db.Messages
                    .Where(m => m.SentAt >= startDate && m.SentAt <= endDate)
                    .GroupBy(m => m.SentAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("d MMM"),
                        MessageCount = g.Count()
                    })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date,
                    [e => e.Sum(f => (double)f.MessageCount)],
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily Messages Sent").Height(Size.Units(80));

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