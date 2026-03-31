namespace IvyAskStatistics.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    private const float ContentWidth = 0.95f;

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();

        var statsQuery = UseQuery<DashboardStats, int>(
            key: 0,
            fetcher: async (_, ct) => await LoadStatsAsync(factory, ct),
            options: new QueryOptions { KeepPrevious = true });

        var stats = statsQuery.Value;

        if (statsQuery.Loading && stats == null)
            return Layout.Center()
                   | new Icon(Icons.Loader)
                   | Text.Muted("Loading dashboard…");

        if (stats == null)
            return Layout.Center() | Text.Muted("No data yet. Run some tests first.");

        // ── KPI cards ────────────────────────────────────────────────────────
        var kpiRow = Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(ContentWidth))
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(stats.TotalRuns.ToString("N0"))
                    | Text.Block("questions tested across all runs").Muted()
            ).Title("Total Runs").Icon(Icons.Play)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(stats.AnswerRate.ToString("F1") + "%")
                    | Text.Block($"{stats.Answered} answered / {stats.NoAnswer} no answer / {stats.Errors} errors").Muted()
            ).Title("Answer Rate").Icon(Icons.CircleCheck)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(stats.AvgResponseMs + " ms")
                    | Text.Block($"fastest {stats.MinResponseMs} ms · slowest {stats.MaxResponseMs} ms").Muted()
            ).Title("Avg Response Time").Icon(Icons.Timer)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(stats.WorstWidget)
                    | Text.Block($"{stats.WorstWidgetRate:F1}% answer rate · most unanswered").Muted()
            ).Title("Weakest Widget").Icon(Icons.CircleX);

        // ── Pie chart: overall distribution ──────────────────────────────────
        var distributionData = new[]
        {
            new { Label = "Answered",  Count = stats.Answered },
            new { Label = "No answer", Count = stats.NoAnswer },
            new { Label = "Error",     Count = stats.Errors   }
        }.Where(x => x.Count > 0).ToList();

        var pieChart = distributionData.ToPieChart(
            dimension: x => x.Label,
            measure: x => x.Sum(f => f.Count),
            PieChartStyles.Dashboard,
            new PieChartTotal(stats.TotalRuns.ToString("N0"), "Total"));

        // ── Bar chart: answer rate per widget (worst first) ───────────────────
        var answerRateData = stats.WidgetStats
            .OrderBy(w => w.AnswerRate)
            .ToList();

        var answerRateChart = answerRateData.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Answer rate %", x => x.Sum(f => f.AnswerRate));

        // ── Bar chart: avg response time per widget (slowest first) ───────────
        var responseTimeData = stats.WidgetStats
            .OrderByDescending(w => w.AvgMs)
            .ToList();

        var responseTimeChart = responseTimeData.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Avg ms", x => x.Sum(f => f.AvgMs));

        // ── Bar chart: results by difficulty ─────────────────────────────────
        var diffData = stats.DifficultyStats;

        var diffChart = diffData.ToBarChart()
            .Dimension("Difficulty", x => x.Difficulty)
            .Measure("Answered",  x => x.Sum(f => f.Answered))
            .Measure("No answer", x => x.Sum(f => f.NoAnswer))
            .Measure("Error",     x => x.Sum(f => f.Errors));

        // ── Detail table ──────────────────────────────────────────────────────
        var tableRows = stats.WidgetStats
            .OrderBy(w => w.AnswerRate)
            .AsQueryable()
            .ToDataTable(r => r.Widget)
            .Key("dashboard-widget-table")
            .Header(r => r.Widget,      "Widget")
            .Header(r => r.TotalRuns,   "Runs")
            .Header(r => r.Answered,    "Answered")
            .Header(r => r.NoAnswer,    "No Answer")
            .Header(r => r.Errors,      "Errors")
            .Header(r => r.AnswerRate,  "Answer Rate %")
            .Header(r => r.AvgMs,       "Avg ms")
            .Width(r => r.Widget,      Size.Px(160))
            .Width(r => r.TotalRuns,   Size.Px(70))
            .Width(r => r.Answered,    Size.Px(90))
            .Width(r => r.NoAnswer,    Size.Px(100))
            .Width(r => r.Errors,      Size.Px(70))
            .Width(r => r.AnswerRate,  Size.Px(120))
            .Width(r => r.AvgMs,       Size.Px(80))
            .Config(c =>
            {
                c.AllowSorting   = true;
                c.AllowFiltering = true;
                c.ShowSearch     = true;
            });

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter).Height(Size.Full())
               | kpiRow
               | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(ContentWidth))
                   | new Card(pieChart).Title("Result Distribution")
                   | new Card(diffChart).Title("Results by Difficulty"))
               | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(ContentWidth))
                   | new Card(answerRateChart).Title("Answer Rate by Widget (worst first)")
                   | new Card(responseTimeChart).Title("Avg Response Time by Widget (slowest first)"))
               | (Layout.Vertical().Gap(2).Width(Size.Fraction(ContentWidth))
                   | new Card(tableRows).Title("Per-Widget Breakdown"));
    }

    private static async Task<DashboardStats?> LoadStatsAsync(AppDbContextFactory factory, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var results = await ctx.Questions
            .AsNoTracking()
            .Where(q => q.LastRunStatus != null)
            .ToListAsync(ct);

        if (results.Count == 0) return null;

        var totalRuns  = results.Count;
        var answered   = results.Count(r => r.LastRunStatus == "success");
        var noAnswer   = results.Count(r => r.LastRunStatus == "no_answer");
        var errors     = results.Count(r => r.LastRunStatus == "error");
        var answerRate = totalRuns > 0 ? answered * 100.0 / totalRuns : 0;
        var avgMs      = (int)results.Average(r => r.LastRunResponseTimeMs ?? 0);
        var minMs      = results.Min(r => r.LastRunResponseTimeMs ?? 0);
        var maxMs      = results.Max(r => r.LastRunResponseTimeMs ?? 0);

        var widgetStats = results
            .GroupBy(r => r.Widget)
            .Select(g =>
            {
                var total = g.Count();
                var ans   = g.Count(r => r.LastRunStatus == "success");
                var noAns = g.Count(r => r.LastRunStatus == "no_answer");
                var err   = g.Count(r => r.LastRunStatus == "error");
                var rate  = total > 0 ? Math.Round(ans * 100.0 / total, 1) : 0;
                var avg   = (int)g.Average(r => r.LastRunResponseTimeMs ?? 0);
                return new WidgetStatRow(g.Key, total, ans, noAns, err, rate, avg);
            })
            .OrderBy(w => w.Widget)
            .ToList();

        var worstWidget = widgetStats.MinBy(w => w.AnswerRate);

        var diffStats = results
            .GroupBy(r => r.Difficulty)
            .Select(g =>
            {
                var ans   = g.Count(r => r.LastRunStatus == "success");
                var noAns = g.Count(r => r.LastRunStatus == "no_answer");
                var err   = g.Count(r => r.LastRunStatus == "error");
                return new DifficultyStatRow(g.Key, ans, noAns, err);
            })
            .OrderBy(d => d.Difficulty == "easy" ? 0 : d.Difficulty == "medium" ? 1 : 2)
            .ToList();

        return new DashboardStats(
            TotalRuns:       totalRuns,
            Answered:        answered,
            NoAnswer:        noAnswer,
            Errors:          errors,
            AnswerRate:      Math.Round(answerRate, 1),
            AvgResponseMs:   avgMs,
            MinResponseMs:   minMs,
            MaxResponseMs:   maxMs,
            WorstWidget:     worstWidget?.Widget ?? "—",
            WorstWidgetRate: worstWidget?.AnswerRate ?? 0,
            WidgetStats:     widgetStats,
            DifficultyStats: diffStats);
    }
}

internal record DashboardStats(
    int    TotalRuns,
    int    Answered,
    int    NoAnswer,
    int    Errors,
    double AnswerRate,
    int    AvgResponseMs,
    int    MinResponseMs,
    int    MaxResponseMs,
    string WorstWidget,
    double WorstWidgetRate,
    List<WidgetStatRow>     WidgetStats,
    List<DifficultyStatRow> DifficultyStats);

internal record WidgetStatRow(
    string Widget,
    int    TotalRuns,
    int    Answered,
    int    NoAnswer,
    int    Errors,
    double AnswerRate,
    int    AvgMs);

internal record DifficultyStatRow(
    string Difficulty,
    int    Answered,
    int    NoAnswer,
    int    Errors);
