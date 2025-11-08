using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.ChartBar, path: ["Apps"])]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var range = this.UseState(() => (fromDate:DateTime.Today.Date.AddDays(-30), toDate:DateTime.Today.Date));

        var header = Layout.Horizontal().Align(Align.Right) | range.ToDateRangeInput();
        
        var fromDate = range.Value.fromDate;
        var toDate = range.Value.toDate;
        
        var metrics =
                Layout.Grid().Columns(4)
                | new TotalInvestmentAmountMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfDealsClosedMetricView(fromDate, toDate).Key(fromDate, toDate) | new AverageDealSizeMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfActiveStartupsMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfFoundersEngagedMetricView(fromDate, toDate).Key(fromDate, toDate) | new NumberOfPartnersEngagedMetricView(fromDate, toDate).Key(fromDate, toDate) 
            ;
            
        var charts = 
                Layout.Grid().Columns(3)
                | new DailyDealAmountsLineChartView(fromDate, toDate).Key(fromDate, toDate) | new IndustryDistributionOfStartupsPieChartView(fromDate, toDate).Key(fromDate, toDate) | new GenderDistributionOfFoundersPieChartView(fromDate, toDate).Key(fromDate, toDate) 
            ;

        return Layout.Horizontal().Align(Align.Center) | 
               new HeaderLayout(header, Layout.Vertical() 
                            | metrics
                            | charts
                ).Width(Size.Full().Max(300));
    }
}