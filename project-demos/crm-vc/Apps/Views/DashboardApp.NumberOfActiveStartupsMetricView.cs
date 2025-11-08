/*
The total number of startups with deals during the selected date range.
COUNT(DISTINCT Deal.StartupId WHERE Deal.DealDate BETWEEN StartDate AND EndDate)
*/
namespace Vc.Apps.Views;

public class NumberOfActiveStartupsMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();

        async Task<MetricRecord> CalculateNumberOfActiveStartups()
        {
            await using var db = factory.CreateDbContext();

            var currentPeriodStartups = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .Select(d => d.StartupId)
                .Distinct()
                .CountAsync();

            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);

            var previousPeriodStartups = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .Select(d => d.StartupId)
                .Distinct()
                .CountAsync();

            if (previousPeriodStartups == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentPeriodStartups.ToString("N0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }

            double? trend = ((double)currentPeriodStartups - previousPeriodStartups) / previousPeriodStartups;
            var goal = previousPeriodStartups * 1.1;
            double? goalAchievement = goal > 0 ? currentPeriodStartups / goal : null;

            return new MetricRecord(
                MetricFormatted: currentPeriodStartups.ToString("N0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N0")
            );
        }

        return new MetricView(
            "Number of Active Startups",
            Icons.Building,
            CalculateNumberOfActiveStartups
        );
    }
}