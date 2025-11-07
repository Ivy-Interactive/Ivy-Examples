/*
Displays the proportion of startups in each industry.
SELECT Industry.Name AS IndustryName, COUNT(Startup.Id) AS StartupCount FROM StartupIndustries JOIN Industries ON StartupIndustries.IndustryId = Industries.Id GROUP BY Industry.Name
*/
namespace Vc.Apps.Views;

public class IndustryDistributionOfStartupsPieChartView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
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
                    .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                    .SelectMany(s => s.Industries)
                    .GroupBy(i => i.Name)
                    .Select(g => new IndustryData { IndustryName = g.Key, StartupCount = g.Count() })
                    .ToListAsync();

                var totalStartups = data.Sum(e => e.StartupCount);

                PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalStartups), "Startups");

                chart.Set(data.ToPieChart(
                    dimension: e => e.IndustryName,
                    measure: e => e.Sum(f => f.StartupCount),
                    PieChartStyles.Dashboard,
                    total));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Industry Distribution of Startups").Height(Size.Units(80));

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

    private class IndustryData
    {
        public string IndustryName { get; set; }
        public int StartupCount { get; set; }
    }
}