using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.ChartBar, path: ["Apps"])]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        // Set initial date (e.g., 01.11.2025)
        var initialDate = new DateTime(2025, 11, 1);
        var range = this.UseState(() => (fromDate: initialDate, toDate: initialDate.AddDays(30)));
        
        var header = Layout.Horizontal().Align(Align.Right)
                    | range.ToDateRangeInput();
        
        var fromDate = range.Value.fromDate;
        var toDate = range.Value.toDate;
        
        var metrics =
                Layout.Grid().Columns(4)
| new TotalSalesRevenueMetricView(fromDate, toDate).Key(fromDate, toDate)| new NumberOfLeadsMetricView(fromDate, toDate).Key(fromDate, toDate)| new ConversionRateMetricView(fromDate, toDate).Key(fromDate, toDate)| new AverageLeadResponseTimeMetricView(fromDate, toDate).Key(fromDate, toDate)| new NumberOfTasksCompletedMetricView(fromDate, toDate).Key(fromDate, toDate)| new CustomerRetentionRateMetricView(fromDate, toDate).Key(fromDate, toDate)| new TotalMessagesSentMetricView(fromDate, toDate).Key(fromDate, toDate)| new AverageVehiclePriceMetricView(fromDate, toDate).Key(fromDate, toDate)            ;

        var charts =
                Layout.Grid().Columns(3)
| new DailyLeadsCreatedLineChartView(fromDate, toDate).Key(fromDate, toDate)| new DailyMessagesSentLineChartView(fromDate, toDate).Key(fromDate, toDate)| new LeadConversionBySourcePieChartView(fromDate, toDate).Key(fromDate, toDate)| new DailyCallDurationsLineChartView(fromDate, toDate).Key(fromDate, toDate)| new TaskCompletionRateLineChartView(fromDate, toDate).Key(fromDate, toDate)| new VehicleStatusDistributionPieChartView(fromDate, toDate).Key(fromDate, toDate)            ;

        return Layout.Horizontal().Align(Align.Center) | 
               new HeaderLayout(header, Layout.Vertical() 
                            | metrics
                            | charts
                ).Width(Size.Full().Max(300));
    }
}