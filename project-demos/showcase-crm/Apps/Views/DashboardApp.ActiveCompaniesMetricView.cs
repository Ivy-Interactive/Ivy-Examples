/*
The total number of companies that have had activity (new leads, contacts, or deals) during the selected period.
COUNT(DISTINCT Company.Id) WHERE Company has Lead.CreatedAt OR Contact.CreatedAt OR Deal.CreatedAt within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class ActiveCompaniesMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object Build()
    {
        return new MetricView(
            "Active Companies",
            Icons.Building,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(ActiveCompaniesMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodActiveCompanies = await db.Companies
                    .Where(c =>
                        c.Leads.Any(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate) ||
                        c.Contacts.Any(co => co.CreatedAt >= fromDate && co.CreatedAt <= toDate) ||
                        c.Deals.Any(d => d.CreatedAt >= fromDate && d.CreatedAt <= toDate))
                    .CountAsync(ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodActiveCompanies = await db.Companies
                    .Where(c =>
                        c.Leads.Any(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate) ||
                        c.Contacts.Any(co => co.CreatedAt >= previousFromDate && co.CreatedAt <= previousToDate) ||
                        c.Deals.Any(d => d.CreatedAt >= previousFromDate && d.CreatedAt <= previousToDate))
                    .CountAsync(ct);

                if (previousPeriodActiveCompanies == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodActiveCompanies.ToString("N0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = ((double)currentPeriodActiveCompanies - previousPeriodActiveCompanies) / previousPeriodActiveCompanies;

                var goal = previousPeriodActiveCompanies * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodActiveCompanies / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodActiveCompanies.ToString("N0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("N0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}