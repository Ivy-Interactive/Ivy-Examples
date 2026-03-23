/*
View the distribution of deals across different stages to understand pipeline health
GROUP deals BY Stage.DescriptionText WHERE CreatedAt BETWEEN startDate AND endDate, COUNT(*) AS stage_count
*/
namespace ShowcaseCrm.Apps.Views;

public class DealPipelineByStagePieChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    private record ChartData(string Stage, double Count);

    public override object? Build()
    {
        var query = UseChartData(Context);
        var card = new Card().Title("Deal Pipeline by Stage").Height(Size.Units(80));

        if (query.Error != null)
        {
            return card | new ErrorTeaserView(query.Error);
        }

        if (query.Loading || query.Value == null)
        {
            return card | new Skeleton();
        }

        var totalDeals = query.Value.Sum(f => f.Count);
        PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalDeals), "Deals");

        var chart = query.Value.ToPieChart(
            dimension: e => e.Stage,
            measure: e => e.Sum(f => f.Count),
            PieChartStyles.Dashboard,
            total);

        return card | chart;
    }

    private QueryResult<ChartData[]> UseChartData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(DealPipelineByStagePieChartView), startDate, endDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var data = await db.Deals
                    .Where(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate)
                    .GroupBy(d => d.Stage.DescriptionText)
                    .Select(g => new ChartData(
                        g.Key,
                        g.Count()
                    ))
                    .ToArrayAsync(ct);

                return data;
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}