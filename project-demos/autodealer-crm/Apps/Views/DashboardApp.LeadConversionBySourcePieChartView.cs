/*
Shows the distribution of leads converted by their source channel.
SELECT SourceChannel.DescriptionText AS Source, COUNT(*) AS LeadCount FROM Leads INNER JOIN SourceChannels ON Leads.SourceChannelId = SourceChannels.Id WHERE Leads.CreatedAt BETWEEN @StartDate AND @EndDate GROUP BY SourceChannel.DescriptionText
*/
namespace AutodealerCrm.Apps.Views;

public class LeadConversionBySourcePieChartView(DateTime startDate, DateTime endDate) : ViewBase
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
                var data = await db.Leads
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                    .GroupBy(l => l.SourceChannel.DescriptionText)
                    .Select(g => new
                    {
                        Source = g.Key,
                        LeadCount = g.Count()
                    })
                    .ToListAsync();

                var totalLeads = data.Sum(d => (double)d.LeadCount);

                PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalLeads), "Leads");

                chart.Set(data.ToPieChart(
                    dimension: e => e.Source,
                    measure: e => e.Sum(x => (double)x.LeadCount),
                    PieChartStyles.Dashboard,
                    total));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Lead Conversion by Source").Height(Size.Units(80));

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