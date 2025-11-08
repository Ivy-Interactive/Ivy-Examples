/*
Tracks the total deal amounts closed each day within the selected date range.
SELECT DealDate AS Date, SUM(Amount) AS TotalAmount FROM Deals WHERE DealDate BETWEEN @StartDate AND @EndDate GROUP BY DealDate ORDER BY DealDate
*/
namespace Vc.Apps.Views;

public class DailyDealAmountsLineChartView(DateTime startDate, DateTime endDate) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<VcContextFactory>();
        var chart = this.UseState<object?>((object?)null!);
        var exception = UseState<Exception?>((Exception?)null!);

        this.UseEffect(async () =>
        {
            try
            {
                var db = factory.CreateDbContext();
                var data = await db.Deals
                    .Where(d => d.DealDate >= startDate && d.DealDate <= endDate)
                    .GroupBy(d => d.DealDate!.Value.Date)
                    .Select(g => new DailyDealData
                    {
                        Date = g.Key.ToString("d MMM"),
                        TotalAmount = g.Sum(d => (double)d.Amount!)
                    })
                    .ToListAsync();

                chart.Set(data.ToLineChart(
                    e => e.Date, 
                    [e => e.Sum(f => f.TotalAmount)], 
                    LineChartStyles.Dashboard));
            }
            catch (Exception ex)
            {
                exception.Set(ex);
            }
        }, []);

        var card = new Card().Title("Daily Deal Amounts").Height(Size.Units(80));

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

    private class DailyDealData
    {
        public string Date { get; set; } = null!;
        public double TotalAmount { get; set; }
    }
}