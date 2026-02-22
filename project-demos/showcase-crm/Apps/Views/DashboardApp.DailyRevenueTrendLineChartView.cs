/*
Track daily revenue from closed won deals to monitor sales performance.
GROUP deals BY DATE(CloseDate) WHERE Stage=Closed Won AND CloseDate BETWEEN startDate AND endDate, SUM(Amount)
*/
namespace ShowcaseCrm.Apps.Views;

public class DailyRevenueTrendLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    private record ChartData(string Date, double DailyRevenue);

    public override object Build()
    {
        var query = UseChartData(Context);
        var card = new Card().Title("Daily Revenue (Closed Won)").Height(Size.Units(80));

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
            [e => e.Sum(f => f.DailyRevenue)],
            LineChartStyles.Dashboard);

        return card | chart;
    }

    private QueryResult<ChartData[]> UseChartData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(DailyRevenueTrendLineChartView), startDate, endDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var data = await db.Deals
                    .Where(d => d.Stage.DescriptionText == "Closed Won"
                        && d.CloseDate >= startDate && d.CloseDate <= endDate
                        && d.Amount.HasValue)
                    .GroupBy(d => d.CloseDate!.Value.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ChartData(
                        g.Key.ToString("d MMM"),
                        g.Sum(d => (double)d.Amount!.Value)
                    ))
                    .ToArrayAsync(ct);

                return data;
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}
