using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.ChartBar, group: ["Apps"])]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var range = this.UseState(() =>
        {
            var initialDate = new DateTime(2025, 11, 1);
            return (fromDate: initialDate, toDate: initialDate.AddDays(30));
        });

        var header = Layout.Horizontal().AlignContent(Align.Right)
                    | range.ToDateRangeInput();

        var fromDate = range.Value.fromDate;
        var toDate = range.Value.toDate;

        var metrics =
                Layout.Grid().Columns(4)
| new TotalSalesRevenueMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfLeadsMetricView(fromDate, toDate).Key(fromDate, toDate) | new ConversionRateMetricView(fromDate, toDate).Key(fromDate, toDate) | new AverageLeadResponseTimeMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfTasksCompletedMetricView(fromDate, toDate).Key(fromDate, toDate) | new CustomerRetentionRateMetricView(fromDate, toDate).Key(fromDate, toDate) | new TotalMessagesSentMetricView(fromDate, toDate).Key(fromDate, toDate) | new AverageVehiclePriceMetricView(fromDate, toDate).Key(fromDate, toDate);

        var charts =
                Layout.Grid().Columns(3)
| new DailyLeadsCreatedLineChartView(fromDate, toDate).Key(fromDate, toDate) | new DailyMessagesSentLineChartView(fromDate, toDate).Key(fromDate, toDate) | new LeadConversionBySourcePieChartView(fromDate, toDate).Key(fromDate, toDate) | new DailyCallDurationsLineChartView(fromDate, toDate).Key(fromDate, toDate) | new TaskCompletionRateLineChartView(fromDate, toDate).Key(fromDate, toDate) | new VehicleStatusDistributionPieChartView(fromDate, toDate).Key(fromDate, toDate);

        return Layout.Horizontal().AlignContent(Align.Center) |
               new HeaderLayout(header, Layout.Vertical()
                            | metrics
                            | charts
                ).Width(Size.Full().Max(300));
    }
}