namespace IvyBenchmark.Apps;

public class StartupChartView(List<VersionMetricRow> data) : ViewBase
{
    public override object? Build()
    {
        var card = new Card().Title("Startup Time by Version").Height(Size.Units(80));

        if (data.Count == 0)
            return card | Text.Muted("No startup data available.");

        var chart = data.ToBarChart()
            .Dimension("Version", e => e.Version)
            .Measure("Startup (ms)", e => e.Sum(f => f.ValueMs));

        return card | chart;
    }
}
