/*
Track the number of new deals created each day to monitor sales activity and pipeline growth
GROUP deals BY DATE(CreatedAt) WHERE CreatedAt BETWEEN startDate AND endDate, COUNT(*) AS daily_count, ORDER BY date
*/
namespace ShowcaseCrm.Apps.Views;

public class DailyDealCreationTrendLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    private record ChartData(string Date, double DailyCount);

    public override object Build()
    {
        var query = UseChartData(Context);
        var card = new Card().Title("Daily Deal Creation Trend").Height(Size.Units(80));

        if (query.Error != null)
        {
            return card | new ErrorTeaserView(query.Error);
        }

        if (query.Loading || query.Value == null)
        {
            return card | new Skeleton();
        }

        var chart = query.Value.ToLineChart(
            e => e.Date,
            [e => e.Sum(f => f.DailyCount)],
            LineChartStyles.Dashboard);

        return card | chart;
    }

    private QueryResult<ChartData[]> UseChartData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(DailyDealCreationTrendLineChartView), startDate, endDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var data = await db.Deals
                    .Where(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate)
                    .GroupBy(d => d.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartData(
                        g.Key.ToString("d MMM"),
                        g.Count()
                    ))
                    .ToArrayAsync(ct);

                return data;
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}