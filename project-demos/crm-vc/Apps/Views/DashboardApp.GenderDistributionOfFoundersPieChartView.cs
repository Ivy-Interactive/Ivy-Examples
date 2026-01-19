/*
Represents the gender distribution of founders.
SELECT Gender.DescriptionText AS Gender, COUNT(Founder.Id) AS FounderCount FROM Founders JOIN Genders ON Founders.GenderId = Genders.Id GROUP BY Gender.DescriptionText
*/
namespace Vc.Apps.Views;

public class GenderDistributionOfFoundersPieChartView(DateTime fromDate, DateTime toDate) : ViewBase
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
                var data = await db.Founders
                    .Where(f => f.CreatedAt >= fromDate && f.CreatedAt <= toDate)
                    .GroupBy(f => f.Gender.DescriptionText)
                    .Select(g => new GenderData { Gender = g.Key, FounderCount = g.Count() })
                    .ToListAsync();

                var totalFounders = data.Sum(e => (double)e.FounderCount);

                PieChartTotal total = new(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", totalFounders), "Founders");

                chart.Set(data.ToPieChart(
                    dimension: e => e.Gender,
                    measure: e => e.Sum(f => (double)f.FounderCount),
                    PieChartStyles.Dashboard,
                    total));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Gender Distribution of Founders").Height(Size.Units(80));

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

    private class GenderData
    {
        public string Gender { get; set; }
        public int FounderCount { get; set; }
    }
}