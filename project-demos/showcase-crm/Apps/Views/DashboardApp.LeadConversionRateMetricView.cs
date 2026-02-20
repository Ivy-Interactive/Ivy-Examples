/*
The percentage of leads that converted into closed deals. Measures the quality of leads and sales process efficiency.
(COUNT(Deal WHERE Deal.LeadId IS NOT NULL AND Deal.Stage indicates closed/won AND Deal.CloseDate is within date range) / COUNT(Lead WHERE Lead.CreatedAt is within date range)) * 100
*/
namespace ShowcaseCrm.Apps.Views;

public class LeadConversionRateMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "Lead Conversion Rate",
            Icons.Target,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(LeadConversionRateMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodDeals = await db.Deals
                    .Where(d => d.LeadId != null 
                                && d.Stage.DescriptionText.ToLower().Contains("closed/won") 
                                && d.CloseDate >= fromDate 
                                && d.CloseDate <= toDate)
                    .CountAsync(ct);

                var currentPeriodLeads = await db.Leads
                    .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                    .CountAsync(ct);

                double? currentConversionRate = currentPeriodLeads > 0 
                    ? (double?)((double)currentPeriodDeals / currentPeriodLeads * 100) 
                    : null;

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodDeals = await db.Deals
                    .Where(d => d.LeadId != null 
                                && d.Stage.DescriptionText.ToLower().Contains("closed/won") 
                                && d.CloseDate >= previousFromDate 
                                && d.CloseDate <= previousToDate)
                    .CountAsync(ct);

                var previousPeriodLeads = await db.Leads
                    .Where(l => l.CreatedAt >= previousFromDate && l.CreatedAt <= previousToDate)
                    .CountAsync(ct);

                double? previousConversionRate = previousPeriodLeads > 0 
                    ? (double)previousPeriodDeals / previousPeriodLeads * 100 
                    : null;

                if (previousConversionRate == null || previousConversionRate == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentConversionRate?.ToString("N2") + "%",
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = (currentConversionRate - previousConversionRate) / previousConversionRate;

                var goal = previousConversionRate * 1.1;
                double? goalAchievement = goal > 0 ? currentConversionRate / goal : null;

                return new MetricRecord(
                    MetricFormatted: currentConversionRate?.ToString("N2") + "%",
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: (goal ?? 0).ToString("N2") + "%"
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}