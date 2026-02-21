/*
Shows the distribution of leads by their source channel.
GROUP leads BY Source WHERE CreatedAt BETWEEN startDate AND endDate
*/
namespace ShowcaseCrm.Apps.Views;

public class LeadsBySourcePieChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    private record ChartData(string Source, double Count);

    public override object? Build()
    {
        var query = UseChartData(Context);
        var card = new Card().Title("Leads by Source").Height(Size.Units(80));

        if (query.Error != null)
        {
            return card | new ErrorTeaserView(query.Error);
        }

        if (query.Loading || query.Value == null)
        {
            return card | new Skeleton();
        }

        var totalLeads = query.Value.Sum(f => f.Count);
        PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalLeads), "Leads");

        var chart = query.Value.ToPieChart(
            dimension: e => e.Source,
            measure: e => e.Sum(f => f.Count),
            PieChartStyles.Dashboard,
            total);

        return card | chart;
    }

    private QueryResult<ChartData[]> UseChartData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(LeadsBySourcePieChartView), startDate, endDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var data = await db.Leads
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                    .GroupBy(l => l.Source ?? "Unknown")
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
