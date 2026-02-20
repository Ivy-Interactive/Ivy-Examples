using System.ComponentModel;
using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.ChartBar, path: ["Apps"])]
public class DashboardApp : ViewBase
{
    private enum DateRange
    {
        [Description("7d")]
        Last7Days = 1,
        [Description("14d")]
        Last14Days = 2,
        [Description("30d")]
        Last30Days = 3
    }

    private (DateTime fromDate, DateTime toDate) GetDateRange(DateRange range)
    {
        var toDate = DateTime.UtcNow.Date.AddDays(1);
        DateTime fromDate = range switch
        {
            DateRange.Last7Days => toDate.AddDays(-7),
            DateRange.Last14Days => toDate.AddDays(-14),
            DateRange.Last30Days => toDate.AddDays(-30),
            _ => toDate.AddDays(-30)
        };
        return (fromDate, toDate);
    }

    public override object? Build()
    {
        var range = UseState<DateRange>(DateRange.Last30Days);

        var header = Layout.Horizontal()
                     | range.ToSelectInput().Variant(SelectInputs.Toggle).Small()
            ;

        var (fromDate, toDate) = GetDateRange(range.Value);

        var metrics =
                Layout.Grid().Columns(4)
| new TotalRevenueMetricView(fromDate, toDate)| new NewLeadsGeneratedMetricView(fromDate, toDate)| new DealsClosedMetricView(fromDate, toDate)| new AverageDealSizeMetricView(fromDate, toDate)| new LeadConversionRateMetricView(fromDate, toDate)| new ActiveCompaniesMetricView(fromDate, toDate)| new NewContactsAddedMetricView(fromDate, toDate)| new PipelineValueMetricView(fromDate, toDate)            ;

        var charts =
                Layout.Grid().Columns(3)
| new DailyDealCreationTrendLineChartView(fromDate, toDate)| new DailyLeadGenerationLineChartView(fromDate, toDate)| new DealPipelineByStagePieChartView(fromDate, toDate)| new DailyRevenueTrendLineChartView(fromDate, toDate)| new LeadStatusDistributionPieChartView(fromDate, toDate)| new DailyCompanyRegistrationLineChartView(fromDate, toDate)            ;

        var body = Layout.TopCenter()
                   | (Layout.Vertical().Width(Size.Full().Max(300)).TopMargin(10)
                      | metrics
                      | charts)
            ;

        return new HeaderLayout(header, body);
    }
}
