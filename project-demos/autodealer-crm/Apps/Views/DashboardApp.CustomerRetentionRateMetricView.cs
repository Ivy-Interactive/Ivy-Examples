/*
The percentage of returning customers within the selected date range.
(COUNT(DISTINCT Customer.Id WHERE Customer.Id IN (SELECT DISTINCT Lead.CustomerId FROM Lead WHERE Lead.CreatedAt BETWEEN StartDate AND EndDate)) / COUNT(Customer.Id WHERE Customer.CreatedAt < StartDate)) * 100
*/
namespace AutodealerCrm.Apps.Views;

public class CustomerRetentionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        QueryResult<MetricRecord> CalculateCustomerRetentionRate(IViewContext ctx)
        {
            var factory = ctx.UseService<AutodealerCrmContextFactory>();
            return ctx.UseQuery<MetricRecord, (DateTime, DateTime)>(
                key: (fromDate, toDate),
                fetcher: async (key, ct) =>
                {
                    var (fd, td) = key;
                    await using var db = factory.CreateDbContext();
                    
                    var currentPeriodReturningCustomers = await db.Customers
                        .Where(c => c.Leads.Any(l => l.CreatedAt >= fd && l.CreatedAt <= td))
                        .Select(c => c.Id)
                        .Distinct()
                        .CountAsync(ct);

                    var totalCustomersBeforePeriod = await db.Customers
                        .Where(c => c.CreatedAt < fd)
                        .CountAsync(ct);

                    var currentRetentionRate = totalCustomersBeforePeriod > 0 
                        ? (double)currentPeriodReturningCustomers / totalCustomersBeforePeriod * 100 
                        : 0.0;

                    var periodLength = td - fd;
                    var previousFromDate = fd.AddDays(-periodLength.TotalDays);
                    var previousToDate = fd.AddDays(-1);

                    var previousPeriodReturningCustomers = await db.Customers
                        .Where(c => c.Leads.Any(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate))
                        .Select(c => c.Id)
                        .Distinct()
                        .CountAsync(ct);

                    var totalCustomersBeforePreviousPeriod = await db.Customers
                        .Where(c => c.CreatedAt < previousFromDate)
                        .CountAsync(ct);

                    double? previousRetentionRate = totalCustomersBeforePreviousPeriod > 0 
                        ? (double?)((double)previousPeriodReturningCustomers / totalCustomersBeforePreviousPeriod * 100)
                        : null;

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
                });
        }

        return new MetricView(
            "Customer Retention Rate",
            Icons.Repeat,
            CalculateCustomerRetentionRate
        );
    }
}