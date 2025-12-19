/*
The percentage of returning customers within the selected date range.
(COUNT(DISTINCT Customer.Id WHERE Customer.Id IN (SELECT DISTINCT Lead.CustomerId FROM Lead WHERE Lead.CreatedAt BETWEEN StartDate AND EndDate)) / COUNT(Customer.Id WHERE Customer.CreatedAt < StartDate)) * 100
*/
namespace AutodealerCrm.Apps.Views;

public class CustomerRetentionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        
        async Task<MetricRecord> CalculateCustomerRetentionRate()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodReturningCustomers = await db.Customers
                .Where(c => c.Leads.Any(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate))
                .Select(c => c.Id)
                .Distinct()
                .CountAsync();

            var totalCustomersBeforePeriod = await db.Customers
                .Where(c => c.CreatedAt < fromDate)
                .CountAsync();

            var currentRetentionRate = totalCustomersBeforePeriod > 0 
                ? (double)currentPeriodReturningCustomers / totalCustomersBeforePeriod * 100 
                : 0.0;

            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);

            var previousPeriodReturningCustomers = await db.Customers
                .Where(c => c.Leads.Any(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate))
                .Select(c => c.Id)
                .Distinct()
                .CountAsync();

            var totalCustomersBeforePreviousPeriod = await db.Customers
                .Where(c => c.CreatedAt < previousFromDate)
                .CountAsync();

            double? previousRetentionRate = totalCustomersBeforePreviousPeriod > 0 
                ? (double?)((double)previousPeriodReturningCustomers / totalCustomersBeforePreviousPeriod * 100 
)                : null;

            if (previousRetentionRate == null || previousRetentionRate == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentRetentionRate.ToString("N2") + "%",
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }

            var trend = (currentRetentionRate - previousRetentionRate.Value) / previousRetentionRate.Value;
            var goal = previousRetentionRate.Value * 1.1;
            var goalAchievement = goal > 0 ? (double?)(currentRetentionRate / goal) : null;

            return new MetricRecord(
                MetricFormatted: currentRetentionRate.ToString("N2") + "%",
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N2") + "%"
            );
        }

        return new MetricView(
            "Customer Retention Rate",
            Icons.Repeat,
            CalculateCustomerRetentionRate
        );
    }
}