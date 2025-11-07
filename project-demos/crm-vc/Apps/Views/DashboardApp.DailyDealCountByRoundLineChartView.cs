/*
Shows the number of deals closed each day, grouped by funding round.
SELECT DealDate AS Date, Round, COUNT(Id) AS DealCount FROM Deals WHERE DealDate BETWEEN @StartDate AND @EndDate GROUP BY DealDate, Round ORDER BY DealDate
*/
namespace Vc.Apps.Views;

public class DailyDealCountByRoundLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<VcContextFactory>();
        var chart = this.UseState<object?>((object?)null!);
        var exception = this.UseState<Exception?>((Exception?)null!);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                var data = await db.Deals
                    .Where(d => d.DealDate >= startDate && d.DealDate <= endDate)
                    .GroupBy(d => new { Date = d.DealDate!.Value.Date, d.Round })
                    .Select(g => new DealRoundData
                    {
                        Date = g.Key.Date.ToString("d MMM"),
                        Round = g.Key.Round,
                        DealCount = g.Count()
                    })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date, 
                    [e => e.Sum(f => f.DealCount)], 
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily Deal Count by Round").Height(Size.Units(80));

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

    private class DealRoundData
    {
        public string Date { get; set; } = null!;
        public string Round { get; set; } = null!;
        public int DealCount { get; set; }
    }
}