using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.ChartBar, path: ["Apps"])]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var initialDate = DateTime.UtcNow.Date.AddDays(-30);
        var range = this.UseState(() => (fromDate: initialDate, toDate: DateTime.UtcNow.Date));

        var header = Layout.Horizontal().Align(Align.Right)
                    | range.ToDateRangeInput();

        var fromDate = range.Value.fromDate;
        var toDate = range.Value.toDate;

        var metrics =
                Layout.Grid().Columns(4)
| new TotalRevenueMetricView(fromDate, toDate).Key(fromDate, toDate)
| new NewLeadsGeneratedMetricView(fromDate, toDate).Key(fromDate, toDate)
| new DealsClosedMetricView(fromDate, toDate).Key(fromDate, toDate)
| new AverageDealSizeMetricView(fromDate, toDate).Key(fromDate, toDate)
| new LeadConversionRateMetricView(fromDate, toDate).Key(fromDate, toDate)
| new ActiveCompaniesMetricView(fromDate, toDate).Key(fromDate, toDate)
| new NewContactsAddedMetricView(fromDate, toDate).Key(fromDate, toDate)
| new PipelineValueMetricView(fromDate, toDate).Key(fromDate, toDate)
            ;

        var charts =
                Layout.Grid().Columns(3)
| new DailyDealCreationTrendLineChartView(fromDate, toDate).Key(fromDate, toDate)
| new DailyLeadGenerationLineChartView(fromDate, toDate).Key(fromDate, toDate)
| new DealPipelineByStagePieChartView(fromDate, toDate).Key(fromDate, toDate)
| new DailyRevenueTrendLineChartView(fromDate, toDate).Key(fromDate, toDate)
| new LeadStatusDistributionPieChartView(fromDate, toDate).Key(fromDate, toDate)
| new LeadsBySourcePieChartView(fromDate, toDate).Key(fromDate, toDate)
            ;

        return Layout.Horizontal().Align(Align.Center) |
               new HeaderLayout(header, Layout.Vertical()
                            | metrics
                            | charts
                ).Width(Size.Full().Max(300));
    }
}
