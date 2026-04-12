using Ivy;
using CTRF.Apps.Models;

namespace CTRF.Apps;

public class ReportDashboardView : ViewBase
{
    private readonly UploadedReport _uploaded;

    public ReportDashboardView(UploadedReport uploaded) => _uploaded = uploaded;

    public override object? Build()
    {
        var report = _uploaded.Report;
        var summary = report.Results.Summary;
        var env = report.Results.Environment;

        return Layout.Vertical().Gap(6)
            | BuildSummaryBar(summary, env)
            | BuildChart(summary)
            | new TestTableView(report.Results.Tests)
            | BuildEnvironmentPanel(env)
            | BuildInsightsPanel(report.Insights);
    }

    private object BuildSummaryBar(CtrfSummary summary, CtrfEnvironment? env)
    {
        var duration = FormatDuration(summary.ComputedDuration);
        var healthy = env?.Healthy;
        var healthBadge = healthy switch
        {
            true => new Badge("Healthy", icon: Icons.Heart).Success(),
            false => new Badge("Unhealthy", icon: Icons.HeartCrack).Destructive(),
            _ => summary.Failed > 0
                ? new Badge("Failing", icon: Icons.CircleAlert).Destructive()
                : new Badge("Passing", icon: Icons.CircleCheck).Success()
        };

        return Layout.Wrap().Gap(3)
            | StatCard("Total Tests", summary.Tests.ToString(), Icons.ListChecks, Colors.Blue)
            | StatCard("Passed", summary.Passed.ToString(), Icons.CircleCheck, Colors.Green)
            | StatCard("Failed", summary.Failed.ToString(), Icons.CircleX, Colors.Red)
            | StatCard("Skipped", summary.Skipped.ToString(), Icons.CircleMinus, Colors.Yellow)
            | StatCard("Pending", summary.Pending.ToString(), Icons.Clock, Colors.Gray)
            | (summary.Flaky.GetValueOrDefault() > 0
                ? StatCard("Flaky", summary.Flaky!.Value.ToString(), Icons.Shuffle, Colors.Orange)
                : StatCard("Flaky", "0", Icons.Shuffle, Colors.Muted))
            | StatCard("Duration", duration, Icons.Timer, Colors.Purple)
            | (new Card()
                .Content(Layout.Vertical().Gap(1).AlignContent(Align.Center)
                    | Text.Muted("Health").Small()
                    | healthBadge)
                .Width(Size.Units(40)));
    }

    private static object StatCard(string label, string value, Icons icon, Colors color)
    {
        return new Card()
            .Content(Layout.Vertical().Gap(1)
                | Text.Muted(label).Small()
                | (Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                    | new Badge(icon: icon).Color(color).Outline()
                    | Text.H3(value)))
            .Width(Size.Units(40));
    }

    private object BuildChart(CtrfSummary summary)
    {
        var data = new List<ChartSlice>();
        if (summary.Passed > 0) data.Add(new ChartSlice { Status = "Passed", Count = summary.Passed });
        if (summary.Failed > 0) data.Add(new ChartSlice { Status = "Failed", Count = summary.Failed });
        if (summary.Skipped > 0) data.Add(new ChartSlice { Status = "Skipped", Count = summary.Skipped });
        if (summary.Pending > 0) data.Add(new ChartSlice { Status = "Pending", Count = summary.Pending });
        if (summary.Other > 0) data.Add(new ChartSlice { Status = "Other", Count = summary.Other });

        if (data.Count == 0) return Layout.Vertical();

        return new Card()
            .Header(Text.H4("Results Breakdown"))
            .Content(data.ToPieChart(
                    e => e.Status,
                    e => e.Sum(x => x.Count),
                    PieChartStyles.Donut)
                .Height(Size.Units(64)));
    }

    private object BuildEnvironmentPanel(CtrfEnvironment? env)
    {
        if (env == null) return Layout.Vertical();

        var details = new List<(string Label, string Value)>();
        if (env.AppName != null) details.Add(("App", $"{env.AppName} {env.AppVersion ?? ""}".Trim()));
        if (env.BuildNumber != null) details.Add(("Build #", env.BuildNumber.Value.ToString()));
        if (env.BuildName != null) details.Add(("Build Name", env.BuildName));
        if (env.BuildUrl != null) details.Add(("Build URL", env.BuildUrl));
        if (env.RepositoryName != null) details.Add(("Repository", env.RepositoryName));
        if (env.BranchName != null) details.Add(("Branch", env.BranchName));
        if (env.Commit != null) details.Add(("Commit", env.Commit));
        if (env.OsPlatform != null) details.Add(("OS", $"{env.OsPlatform} {env.OsVersion ?? ""}".Trim()));
        if (env.TestEnvironment != null) details.Add(("Environment", env.TestEnvironment));

        if (details.Count == 0) return Layout.Vertical();

        var grid = Layout.Grid().Columns(2).Gap(2);
        foreach (var (label, value) in details)
        {
            grid = grid | Text.Muted(label).Bold() | Text.Block(value);
        }

        return new Expandable(Text.H4("Environment"), grid);
    }

    private object BuildInsightsPanel(Dictionary<string, InsightMetric>? insights)
    {
        if (insights == null || insights.Count == 0) return Layout.Vertical();

        var wrap = Layout.Wrap().Gap(3);
        foreach (var (key, metric) in insights)
        {
            var label = FormatInsightKey(key);
            var isRate = key.Contains("Rate", StringComparison.OrdinalIgnoreCase);
            var currentStr = isRate ? $"{metric.Current:P0}" : FormatMetricValue(key, metric.Current);
            var baselineStr = isRate ? $"{metric.Baseline:P0}" : FormatMetricValue(key, metric.Baseline);
            var changeStr = isRate ? $"{metric.Change:+0%;-0%}" : FormatMetricChange(key, metric.Change);

            var isImprovement = key.Contains("fail", StringComparison.OrdinalIgnoreCase) || key.Contains("flaky", StringComparison.OrdinalIgnoreCase)
                ? metric.Change < 0
                : metric.Change > 0;
            var changeColor = metric.Change == 0 ? Colors.Muted : isImprovement ? Colors.Green : Colors.Red;

            wrap = wrap | (new Card()
                .Content(Layout.Vertical().Gap(1)
                    | Text.Muted(label).Small()
                    | Text.H3(currentStr)
                    | (Layout.Horizontal().Gap(2)
                        | Text.Muted($"Baseline: {baselineStr}").Small()
                        | Text.Block(changeStr).Small().Color(changeColor)))
                .Width(Size.Units(48)));
        }

        return new Expandable(Text.H4("Insights"), wrap).Open();
    }

    public static string FormatDuration(long ms)
    {
        if (ms < 1000) return $"{ms}ms";
        var ts = TimeSpan.FromMilliseconds(ms);
        if (ts.TotalMinutes < 1) return $"{ts.TotalSeconds:F1}s";
        if (ts.TotalHours < 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{(int)ts.TotalHours}h {ts.Minutes}m";
    }

    private static string FormatInsightKey(string key)
    {

        var result = System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
        return char.ToUpper(result[0]) + result[1..];
    }

    private static string FormatMetricValue(string key, double value)
    {
        if (key.Contains("Duration", StringComparison.OrdinalIgnoreCase))
            return FormatDuration((long)value);
        return value.ToString("N0");
    }

    private static string FormatMetricChange(string key, double value)
    {
        if (key.Contains("Duration", StringComparison.OrdinalIgnoreCase))
            return (value >= 0 ? "+" : "") + FormatDuration((long)Math.Abs(value));
        return (value >= 0 ? "+" : "") + value.ToString("N0");
    }
}
