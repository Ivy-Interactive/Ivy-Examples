/*
Tracks the percentage of tasks completed daily based on DueDate.
SELECT Date(DueDate) AS Date, (SUM(CASE WHEN Completed = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS CompletionRate FROM Tasks WHERE DueDate BETWEEN @StartDate AND @EndDate GROUP BY Date(DueDate) ORDER BY Date(DueDate)
*/
namespace AutodealerCrm.Apps.Views;

public class TaskCompletionRateLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var chart = UseState<object?>((object?)null);
        var exception = UseState<Exception?>((Exception?)null);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                
                // Filter tasks with DueDate in range and group by date
                var queryResult = await db.Tasks
                    .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= startDate.Date && t.DueDate.Value.Date <= endDate.Date)
                    .GroupBy(t => t.DueDate!.Value.Date)
                    .Select(g => new
                    {
                        DateKey = g.Key,
                        CompletionRate = g.Count() > 0 
                            ? (g.Count(t => t.Completed == true) * 100.0 / g.Count())
                            : 0.0
                    })
                    .ToListAsync();

                var data = queryResult
                    .OrderBy(x => x.DateKey)
                    .Select(x => new
                    {
                        Date = x.DateKey.ToString("d MMM"),
                        CompletionRate = x.CompletionRate
                    })
                    .ToList();

                chart.Set(data.ToLineChart(
                    e => e.Date,
                    [e => e.Sum(f => f.CompletionRate)],
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Task Completion Rate").Height(Size.Units(80));

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