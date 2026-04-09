namespace IvyBenchmark.Apps;

public class LatencyChartView(List<VersionLatencyRow> data) : ViewBase
{
    public override object? Build()
    {
        var card = new Card().Title("Latency: Avg vs P95").Height(Size.Units(80));

        if (data.Count == 0)
            return card | Text.Muted("No latency data available.");

        var chart = data.ToLineChart()
            .Dimension("Version", e => e.Version)
            .Measure("Avg (ms)", e => e.Sum(f => f.AvgMs))
            .Measure("P95 (ms)", e => e.Sum(f => f.P95Ms));

        return card | chart;
    }
}
