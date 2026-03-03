/*
The total number of new contacts created during the selected period. Indicates network growth and relationship building efforts.
COUNT(Contact) WHERE Contact.CreatedAt is within date range
*/
namespace ShowcaseCrm.Apps.Views;

public class NewContactsAddedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        return new MetricView(
            "New Contacts Added",
            Icons.Users,
            UseMetricData
        );
    }

    private QueryResult<MetricRecord> UseMetricData(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();

        return context.UseQuery(
            key: (nameof(NewContactsAddedMetricView), fromDate, toDate),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var currentPeriodContacts = await db.Contacts
                    .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
                    .CountAsync(ct);

                var periodLength = toDate - fromDate;
                var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
                var previousToDate = fromDate.AddDays(-1);

                var previousPeriodContacts = await db.Contacts
                    .Where(c => c.CreatedAt >= previousFromDate && c.CreatedAt <= previousToDate)
                    .CountAsync(ct);

                if (previousPeriodContacts == 0)
                {
                    return new MetricRecord(
                        MetricFormatted: currentPeriodContacts.ToString("N0"),
                        TrendComparedToPreviousPeriod: null,
                        GoalAchieved: null,
                        GoalFormatted: null
                    );
                }

                double? trend = ((double)currentPeriodContacts - previousPeriodContacts) / previousPeriodContacts;

                var goal = previousPeriodContacts * 1.1;
                double? goalAchievement = goal > 0 ? (double?)(currentPeriodContacts / goal ): null;

                return new MetricRecord(
                    MetricFormatted: currentPeriodContacts.ToString("N0"),
                    TrendComparedToPreviousPeriod: trend,
                    GoalAchieved: goalAchievement,
                    GoalFormatted: goal.ToString("N0")
                );
            },
            options: new QueryOptions { Expiration = TimeSpan.FromMinutes(5) }
        );
    }
}