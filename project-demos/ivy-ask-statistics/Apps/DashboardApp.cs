namespace IvyAskStatistics.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    private const float ContentWidth = 0.95f;

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();

        var selectedVersion = UseState("");
        var refreshToken = UseRefreshToken();

        var versionsQuery = UseQuery<List<RunSummaryRow>, int>(
            key: 0,
            fetcher: async (_, ct) => await LoadRunSummariesAsync(factory, ct),
            options: new QueryOptions { KeepPrevious = true });

        var statsQuery = UseQuery<DashboardStats?, string>(
            key: selectedVersion.Value,
            fetcher: async (version, ct) =>
            {
                if (string.IsNullOrEmpty(version)) return null;
                return await LoadStatsForVersionAsync(factory, version, ct);
            },
            options: new QueryOptions { KeepPrevious = true });

        var versions = versionsQuery.Value ?? [];

        if (versionsQuery.Loading && versions.Count == 0)
            return Layout.Center()
                   | new Icon(Icons.Loader)
                   | Text.Muted("Loading dashboard…");

        if (versions.Count == 0)
            return Layout.Center() | Text.Muted("No test runs yet. Run some tests first.");

        if (string.IsNullOrEmpty(selectedVersion.Value) && versions.Count > 0)
            selectedVersion.Set(versions[0].IvyVersion);

        var versionOptions = versions.Select(v => v.IvyVersion).Distinct().ToArray();
        var stats = statsQuery.Value;

        var versionSelector = Layout.Horizontal().Height(Size.Fit()).Gap(2)
            | Text.Block("Ivy Version:").Muted()
            | selectedVersion.ToSelectInput(versionOptions);

        if (stats == null)
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter).Height(Size.Full())
                   | versionSelector
                   | (Layout.Center() | Text.Muted("No data for this version."));

        var kpiRow = Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(ContentWidth))
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(stats.TotalResults.ToString("N0"))
                    | Text.Block("questions tested").Muted()
            ).Title("Total Questions").Icon(Icons.Play)
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
                    | Text.Block($"{stats.WorstWidgetRate:F1}% answer rate").Muted()
            ).Title("Weakest Widget").Icon(Icons.CircleX);

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
            new PieChartTotal(stats.TotalResults.ToString("N0"), "Total"));

        var answerRateData = stats.WidgetStats
            .OrderBy(w => w.AnswerRate)
            .ToList();

        var answerRateChart = answerRateData.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Answer rate %", x => x.Sum(f => f.AnswerRate));

        var responseTimeData = stats.WidgetStats
            .OrderByDescending(w => w.AvgMs)
            .ToList();

        var responseTimeChart = responseTimeData.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Avg ms", x => x.Sum(f => f.AvgMs));

        var diffData = stats.DifficultyStats;

        var diffChart = diffData.ToBarChart()
            .Dimension("Difficulty", x => x.Difficulty)
            .Measure("Answered",  x => x.Sum(f => f.Answered))
            .Measure("No answer", x => x.Sum(f => f.NoAnswer))
            .Measure("Error",     x => x.Sum(f => f.Errors));

        var widgetTable = stats.WidgetStats
            .OrderBy(w => w.AnswerRate)
            .AsQueryable()
            .ToDataTable(r => r.Widget)
            .Key("dashboard-widget-table")
            .Header(r => r.Widget,     "Widget")
            .Header(r => r.TotalRuns,  "Tested")
            .Header(r => r.Answered,   "Answered")
            .Header(r => r.NoAnswer,   "No Answer")
            .Header(r => r.Errors,     "Errors")
            .Header(r => r.AnswerRate, "Answer Rate %")
            .Header(r => r.AvgMs,      "Avg ms")
            .Width(r => r.Widget,     Size.Px(160))
            .Width(r => r.TotalRuns,  Size.Px(70))
            .Width(r => r.Answered,   Size.Px(90))
            .Width(r => r.NoAnswer,   Size.Px(100))
            .Width(r => r.Errors,     Size.Px(70))
            .Width(r => r.AnswerRate, Size.Px(120))
            .Width(r => r.AvgMs,      Size.Px(80))
            .Config(c =>
            {
                c.AllowSorting   = true;
                c.AllowFiltering = true;
                c.ShowSearch     = true;
            });

        var runHistoryTable = versions.AsQueryable()
            .ToDataTable(r => r.IvyVersion)
            .Key("run-history-table")
            .Header(r => r.IvyVersion,   "Ivy Version")
            .Header(r => r.TotalQuestions, "Questions")
            .Header(r => r.SuccessCount,  "Answered")
            .Header(r => r.NoAnswerCount, "No Answer")
            .Header(r => r.ErrorCount,    "Errors")
            .Header(r => r.SuccessRate,   "Success %")
            .Header(r => r.StartedAt,     "Started")
            .Header(r => r.Duration,      "Duration")
            .Width(r => r.IvyVersion,    Size.Px(120))
            .Width(r => r.TotalQuestions, Size.Px(90))
            .Width(r => r.SuccessCount,  Size.Px(90))
            .Width(r => r.NoAnswerCount, Size.Px(90))
            .Width(r => r.ErrorCount,    Size.Px(70))
            .Width(r => r.SuccessRate,   Size.Px(100))
            .Width(r => r.StartedAt,     Size.Px(170))
            .Width(r => r.Duration,      Size.Px(100))
            .Config(c =>
            {
                c.AllowSorting   = true;
                c.ShowIndexColumn = false;
            });

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter).Height(Size.Full())
               | versionSelector
               | kpiRow
               | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(ContentWidth))
                   | new Card(pieChart).Title("Result Distribution")
                   | new Card(diffChart).Title("Results by Difficulty"))
               | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(ContentWidth))
                   | new Card(answerRateChart).Title("Answer Rate by Widget (worst first)")
                   | new Card(responseTimeChart).Title("Avg Response Time by Widget (slowest first)"))
               | (Layout.Vertical().Gap(2).Width(Size.Fraction(ContentWidth))
                   | new Card(widgetTable).Title("Per-Widget Breakdown"))
               | (Layout.Vertical().Gap(2).Width(Size.Fraction(ContentWidth))
                   | new Card(runHistoryTable).Title("Run History"));
    }

    private static async Task<List<RunSummaryRow>> LoadRunSummariesAsync(
        AppDbContextFactory factory, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var runs = await ctx.TestRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(ct);

        return runs.Select(r =>
        {
            var total = r.TotalQuestions;
            var rate = total > 0 ? Math.Round(r.SuccessCount * 100.0 / total, 1) : 0;
            var duration = r.CompletedAt.HasValue
                ? $"{(r.CompletedAt.Value - r.StartedAt).TotalSeconds:F0}s"
                : "in progress";
            var started = r.StartedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
            return new RunSummaryRow(r.IvyVersion, total, r.SuccessCount, r.NoAnswerCount, r.ErrorCount, rate, started, duration);
        }).ToList();
    }

    private static async Task<DashboardStats?> LoadStatsForVersionAsync(
        AppDbContextFactory factory, string ivyVersion, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var run = await ctx.TestRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IvyVersion == ivyVersion, ct);

        if (run == null) return null;

        var results = await ctx.TestResults
            .AsNoTracking()
            .Include(r => r.Question)
            .Where(r => r.TestRunId == run.Id)
            .ToListAsync(ct);

        if (results.Count == 0) return null;

        var totalResults = results.Count;
        var answered     = results.Count(r => r.IsSuccess);
        var noAnswer     = results.Count(r => !r.IsSuccess && r.HttpStatus == 404);
        var errors       = results.Count(r => !r.IsSuccess && r.HttpStatus != 404);
        var answerRate   = totalResults > 0 ? answered * 100.0 / totalResults : 0;
        var avgMs        = (int)results.Average(r => r.ResponseTimeMs);
        var minMs        = results.Min(r => r.ResponseTimeMs);
        var maxMs        = results.Max(r => r.ResponseTimeMs);

        var widgetStats = results
            .GroupBy(r => r.Question.Widget)
            .Select(g =>
            {
                var total = g.Count();
                var ans   = g.Count(r => r.IsSuccess);
                var noAns = g.Count(r => !r.IsSuccess && r.HttpStatus == 404);
                var err   = g.Count(r => !r.IsSuccess && r.HttpStatus != 404);
                var rate  = total > 0 ? Math.Round(ans * 100.0 / total, 1) : 0;
                var avg   = (int)g.Average(r => r.ResponseTimeMs);
                return new WidgetStatRow(g.Key, total, ans, noAns, err, rate, avg);
            })
            .OrderBy(w => w.Widget)
            .ToList();

        var worstWidget = widgetStats.MinBy(w => w.AnswerRate);

        var diffStats = results
            .GroupBy(r => r.Question.Difficulty)
            .Select(g =>
            {
                var ans   = g.Count(r => r.IsSuccess);
                var noAns = g.Count(r => !r.IsSuccess && r.HttpStatus == 404);
                var err   = g.Count(r => !r.IsSuccess && r.HttpStatus != 404);
                return new DifficultyStatRow(g.Key, ans, noAns, err);
            })
            .OrderBy(d => d.Difficulty == "easy" ? 0 : d.Difficulty == "medium" ? 1 : 2)
            .ToList();

        return new DashboardStats(
            TotalResults:    totalResults,
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

internal record RunSummaryRow(
    string IvyVersion,
    int    TotalQuestions,
    int    SuccessCount,
    int    NoAnswerCount,
    int    ErrorCount,
    double SuccessRate,
    string StartedAt,
    string Duration);

internal record DashboardStats(
    int    TotalResults,
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
