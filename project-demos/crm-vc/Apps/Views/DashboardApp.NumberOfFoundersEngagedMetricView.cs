/*
The total number of founders associated with startups that closed deals during the selected date range.
COUNT(DISTINCT StartupFounder.FounderId WHERE Deal.DealDate BETWEEN StartDate AND EndDate AND Deal.StartupId = StartupFounder.StartupId)
*/
namespace Vc.Apps.Views;

public class NumberOfFoundersEngagedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        
        async Task<MetricRecord> CalculateNumberOfFoundersEngaged()
        {
            await using var db = factory.CreateDbContext();
            
            var currentPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .Include(deal => deal.Startup)
                .ToListAsync();
            
            var currentPeriodStartupIds = currentPeriodDeals
                .Select(d => d.StartupId)
                .Distinct()
                .ToList();
            
            var currentPeriodFounders = await db.Founders
                .Where(f => f.Startups.Any(s => currentPeriodStartupIds.Contains(s.Id)))
                .ToListAsync();
            
            var currentPeriodFounderCount = currentPeriodFounders
                .Select(f => f.Id)
                .Distinct()
                .Count();
            
            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);
            
            var previousPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .Include(deal => deal.Startup)
                .ToListAsync();
            
            var previousPeriodStartupIds = previousPeriodDeals
                .Select(d => d.StartupId)
                .Distinct()
                .ToList();
            
            var previousPeriodFounders = await db.Founders
                .Where(f => f.Startups.Any(s => previousPeriodStartupIds.Contains(s.Id)))
                .ToListAsync();
            
            var previousPeriodFounderCount = previousPeriodFounders
                .Select(f => f.Id)
                .Distinct()
                .Count();

            if (previousPeriodFounderCount == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentPeriodFounderCount.ToString("N0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }
            
            double? trend = ((double)currentPeriodFounderCount - previousPeriodFounderCount) / previousPeriodFounderCount;
            
            var goal = previousPeriodFounderCount * 1.1;
            double? goalAchievement = goal > 0 ? currentPeriodFounderCount / goal : null;
            
            return new MetricRecord(
                MetricFormatted: currentPeriodFounderCount.ToString("N0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N0")
            );
        }

        return new MetricView(
            "Number of Founders Engaged",
            Icons.Users,
            CalculateNumberOfFoundersEngaged
        );
    }
}