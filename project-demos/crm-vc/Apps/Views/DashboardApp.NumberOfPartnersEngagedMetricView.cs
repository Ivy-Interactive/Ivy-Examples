/*
The total number of partners involved in deals during the selected date range.
COUNT(DISTINCT PartnerDeal.PartnerId WHERE Deal.DealDate BETWEEN StartDate AND EndDate)
*/
namespace Vc.Apps.Views;

public class NumberOfPartnersEngagedMetricView(DateTime fromDate, DateTime toDate) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();

        async Task<MetricRecord> CalculateNumberOfPartnersEngaged()
        {
            await using var db = factory.CreateDbContext();

            var currentPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= fromDate && d.DealDate <= toDate)
                .Include(deal => deal.Partners)
                .ToListAsync();

            var currentPeriodPartners = currentPeriodDeals
                .SelectMany(d => d.Partners)
                .Select(p => p.Id)
                .Distinct()
                .Count();

            var periodLength = toDate - fromDate;
            var previousFromDate = fromDate.AddDays(-periodLength.TotalDays);
            var previousToDate = fromDate.AddDays(-1);

            var previousPeriodDeals = await db.Deals
                .Where(d => d.DealDate >= previousFromDate && d.DealDate <= previousToDate)
                .Include(deal => deal.Partners)
                .ToListAsync();

            var previousPeriodPartners = previousPeriodDeals
                .SelectMany(d => d.Partners)
                .Select(p => p.Id)
                .Distinct()
                .Count();

            if (previousPeriodPartners == 0)
            {
                return new MetricRecord(
                    MetricFormatted: currentPeriodPartners.ToString("N0"),
                    TrendComparedToPreviousPeriod: null,
                    GoalAchieved: null,
                    GoalFormatted: null
                );
            }

            double? trend = ((double)currentPeriodPartners - previousPeriodPartners) / previousPeriodPartners;

            var goal = previousPeriodPartners * 1.1;
            double? goalAchievement = goal > 0 ? currentPeriodPartners / goal : null;

            return new MetricRecord(
                MetricFormatted: currentPeriodPartners.ToString("N0"),
                TrendComparedToPreviousPeriod: trend,
                GoalAchieved: goalAchievement,
                GoalFormatted: goal.ToString("N0")
            );
        }

        return new MetricView(
            "Number of Partners Engaged",
            Icons.Handshake,
            CalculateNumberOfPartnersEngaged
        );
    }
}