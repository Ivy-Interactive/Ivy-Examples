/*
The total amount of investments made during the selected date range.
SUM(Deal.Amount WHERE Deal.DealDate BETWEEN StartDate AND EndDate)
*/
namespace Vc.Apps.Views;

public class TotalInvestmentAmountMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        
        async Task<MetricRecord> CalculateTotalInvestmentAmount()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .ToListAsync();
                
            var currentPeriodInvestmentAmount = currentPeriodDeals
                .Sum(d => (double)(d.Amount ?? 0));
                
            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .ToListAsync();
                
            var previousPeriodInvestmentAmount = previousPeriodDeals
                .Sum(d => (double)(d.Amount ?? 0));

            if (previousPeriodInvestmentAmount == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentPeriodInvestmentAmount.ToString("C0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }
            
            double? trend = (currentPeriodInvestmentAmount - previousPeriodInvestmentAmount) / previousPeriodInvestmentAmount;
            
            var goal = previousPeriodInvestmentAmount * 1.1;
            double? goalAchievement = goal > 0 ? currentPeriodInvestmentAmount / goal : null;
            
            return new MetricRecord(
                MetricFormatted: currentPeriodInvestmentAmount.ToString("C0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("C0")
            );
        }

        return new MetricView(
            "Total Investment Amount",
            Icons.DollarSign,
            CalculateTotalInvestmentAmount
        );
    }
}