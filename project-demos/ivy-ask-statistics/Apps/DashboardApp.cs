namespace IvyAskStatistics.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();

        var dashQuery = UseQuery<DashboardPageModel?, int>(
            key: 0,
            fetcher: async (_, ct) => await LoadDashboardPageAsync(factory, ct),
            options: new QueryOptions { KeepPrevious = true, RevalidateOnMount = true },
            tags: ["dashboard-stats"]);

        if (dashQuery.Loading && dashQuery.Value == null)
            return Layout.Vertical().Gap(3).Padding(4).Height(Size.Full())
                   | Text.H3("Dashboard")
                   | Text.Muted("Loading…")
                   | Layout.Center().Height(Size.Units(40)) | new Icon(Icons.Loader);

        if (dashQuery.Value == null)
            return Layout.Vertical().Gap(3).Padding(4).Height(Size.Full()).Align(Align.Center)
                   | new Icon(Icons.LayoutDashboard)
                   | Text.H3("No dashboard data yet")
                   | Text.Block(
                       "Run tests from the Run tab to record results. Statistics and version charts appear after the first completed run.")
                       .Muted()
                       .Width(Size.Fraction(0.5f));

        var page = dashQuery.Value;
        var data = page.Detail;
        var versionTrend = page.VersionTrend;

        var header = Layout.Vertical().Gap(1)
            | Text.H3("Dashboard")
            | Text.Block($"Ivy {page.IvyVersion} · {page.RunStartedAt.ToLocalTime():g}").Muted();

        // ── Level 1: Summary KPIs with deltas (vs previous Ivy version when possible) ──
        var rateStr = $"{data.AnswerRate:F1}%";
        var rateDelta = data.PrevAnswerRate.HasValue
            ? FormatDelta(data.AnswerRate - data.PrevAnswerRate.Value, "%", higherIsBetter: true)
            : Text.Muted("first version");

        var avgMsStr = $"{data.AvgMs} ms";
        var avgMsDelta = data.PrevAvgMs.HasValue
            ? FormatDelta(data.AvgMs - data.PrevAvgMs.Value, "ms", higherIsBetter: false)
            : Text.Muted("first version");

        var failedCount = data.NoAnswer + data.Errors;

        var kpiRow = Layout.Grid().Columns(4).Gap(3).Height(Size.Fit())
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(rateStr)
                    | rateDelta
            ).Title("Success Rate").Icon(Icons.CircleCheck)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(avgMsStr)
                    | avgMsDelta
            ).Title("Avg Response").Icon(Icons.Timer)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(failedCount.ToString())
                    | Text.Block($"{data.NoAnswer} no answer · {data.Errors} errors").Muted()
            ).Title("Failed Questions").Icon(Icons.CircleX)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(data.WorstWidgets.Count > 0 ? data.WorstWidgets[0].Widget : "—")
                    | Text.Block(data.WorstWidgets.Count > 0 ? $"{data.WorstWidgets[0].AnswerRate:F1}% answer rate" : "").Muted()
            ).Title("Weakest Widget").Icon(Icons.Ban);

        // ── Version history (one point = latest completed run per Ivy version) ──
        object versionChartsRow;
        if (versionTrend.Count >= 1)
        {
            var rateByVersion = versionTrend.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Success %", x => x.Sum(f => f.AnswerRate));

            var latencyByVersion = versionTrend.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Avg ms", x => x.Sum(f => f.AvgMs));

            var outcomesByVersion = versionTrend.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Answered", x => x.Sum(f => f.Answered))
                .Measure("No answer", x => x.Sum(f => f.NoAnswer))
                .Measure("Error", x => x.Sum(f => f.Errors));

            versionChartsRow = Layout.Grid().Columns(3).Gap(3).Height(Size.Fit())
                | new Card(rateByVersion).Title("Success rate by Ivy version").Height(Size.Units(70))
                | new Card(latencyByVersion).Title("Avg response by Ivy version").Height(Size.Units(70))
                | new Card(outcomesByVersion).Title("Outcomes by Ivy version").Height(Size.Units(70));
        }
        else
        {
            versionChartsRow = Layout.Vertical();
        }

        // ── Level 2: Problem tables ──
        var worstTable = data.WorstWidgets.AsQueryable()
            .ToDataTable(r => r.Widget)
            .Key("worst-widgets")
            .Header(r => r.Widget, "Widget")
            .Header(r => r.AnswerRate, "Rate %")
            .Header(r => r.Failed, "Failed")
            .Header(r => r.Tested, "Tested")
            .Width(r => r.Widget, Size.Px(140))
            .Width(r => r.AnswerRate, Size.Px(70))
            .Width(r => r.Failed, Size.Px(70))
            .Width(r => r.Tested, Size.Px(70))
            .Config(c => { c.ShowIndexColumn = false; c.AllowSorting = true; });

        var slowestTable = data.SlowestWidgets.AsQueryable()
            .ToDataTable(r => r.Widget)
            .Key("slowest-widgets")
            .Header(r => r.Widget, "Widget")
            .Header(r => r.AvgMs, "Avg ms")
            .Header(r => r.MaxMs, "Max ms")
            .Width(r => r.Widget, Size.Px(140))
            .Width(r => r.AvgMs, Size.Px(80))
            .Width(r => r.MaxMs, Size.Px(80))
            .Config(c => { c.ShowIndexColumn = false; c.AllowSorting = true; });

        var worstChart = data.WorstWidgets.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Answer rate %", x => x.Sum(f => f.AnswerRate));

        var resultDistribution = new[]
        {
            new { Label = "Answered", Count = data.Answered },
            new { Label = "No answer", Count = data.NoAnswer },
            new { Label = "Error", Count = data.Errors }
        }.Where(x => x.Count > 0).ToList();

        var pieChart = resultDistribution.ToPieChart(
            dimension: x => x.Label,
            measure: x => x.Sum(f => f.Count),
            PieChartStyles.Dashboard,
            new PieChartTotal(data.Total.ToString("N0"), "Total"));

        var difficultyChart = data.DifficultyBreakdown.ToBarChart()
            .Dimension("Difficulty", x => x.Difficulty)
            .Measure("Answered", x => x.Sum(f => f.Answered))
            .Measure("No answer", x => x.Sum(f => f.NoAnswer))
            .Measure("Error", x => x.Sum(f => f.Errors));

        var chartsRow = Layout.Grid().Columns(3).Gap(3).Height(Size.Fit())
            | new Card(worstChart).Title("Worst Widgets").Height(Size.Units(70))
            | new Card(difficultyChart).Title("Results by Difficulty").Height(Size.Units(70))
            | new Card(pieChart).Title("Result Distribution").Height(Size.Units(70));

        var problemRow = Layout.Grid().Columns(2).Gap(3).Height(Size.Fit())
            | new Card(worstTable).Title("Worst Widgets (top 10)")
            | new Card(slowestTable).Title("Slowest Widgets (top 10)");

        // ── Level 3: Failed questions debug table ──
        var failedTable = data.FailedQuestions.AsQueryable()
            .ToDataTable()
            .Key("failed-questions")
            .Height(Size.Full())
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Difficulty, "Difficulty")
            .Header(r => r.Question, "Question")
            .Header(r => r.Status, "Status")
            .Header(r => r.ResponseTimeMs, "Time (ms)")
            .Width(r => r.Widget, Size.Px(140))
            .Width(r => r.Difficulty, Size.Px(90))
            .Width(r => r.Status, Size.Px(90))
            .Width(r => r.ResponseTimeMs, Size.Px(90))
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;
                c.ShowIndexColumn = true;
            });

        return Layout.Vertical().Gap(3).Padding(4).Height(Size.Full())
               | header
               | kpiRow
               | versionChartsRow
               | chartsRow
               | problemRow
               | new Card(failedTable).Title($"Failed Questions ({failedCount})");
    }

    private static object FormatDelta(double delta, string unit, bool higherIsBetter)
    {
        if (Math.Abs(delta) < 0.05) return Text.Muted("no change");
        var sign = delta > 0 ? "+" : "";
        var label = unit == "%" ? $"{sign}{delta:F1}{unit}" : $"{sign}{(int)delta} {unit}";
        var isGood = higherIsBetter ? delta > 0 : delta < 0;
        return isGood ? Text.Block(label).Color(Colors.Emerald) : Text.Block(label).Color(Colors.Red);
    }

    private static async Task<DashboardPageModel?> LoadDashboardPageAsync(
        AppDbContextFactory factory, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var runIdsWithData = await ctx.TestResults.AsNoTracking()
            .Select(r => r.TestRunId)
            .Distinct()
            .ToListAsync(ct);

        if (runIdsWithData.Count == 0) return null;

        var runs = await ctx.TestRuns.AsNoTracking()
            .Where(r => r.CompletedAt != null && runIdsWithData.Contains(r.Id))
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(ct);

        if (runs.Count == 0) return null;

        var latestRun = runs[0];

        var seenVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var latestRunPerVersion = new List<TestRunEntity>();
        foreach (var r in runs)
        {
            var v = (r.IvyVersion ?? "").Trim();
            if (string.IsNullOrEmpty(v)) continue;
            if (!seenVersions.Add(v)) continue;
            latestRunPerVersion.Add(r);
        }

        var trendRunIds = latestRunPerVersion.Select(r => r.Id).ToList();
        var trendResults = await ctx.TestResults.AsNoTracking()
            .Include(r => r.Question)
            .Where(r => trendRunIds.Contains(r.TestRunId))
            .ToListAsync(ct);

        // Use TestRun row counters for outcomes/rate so charts match ivy_ask_test_runs (FinalizeRun totals).
        // Row-level ivy_ask_test_results can be short if historical runs failed mid-persist; averages still use rows when present.
        var versionTrend = new List<VersionTrendRow>();
        foreach (var r in latestRunPerVersion)
        {
            var res = trendResults.Where(x => x.TestRunId == r.Id).ToList();
            var answered = r.SuccessCount;
            var noAnswer = r.NoAnswerCount;
            var errors = r.ErrorCount;
            var t = r.TotalQuestions;
            var rate = t > 0 ? Math.Round(answered * 100.0 / t, 1) : 0;
            var avgMs = res.Count > 0 ? Math.Round(res.Average(x => (double)x.ResponseTimeMs), 0) : 0;
            versionTrend.Add(new VersionTrendRow(
                (r.IvyVersion ?? "").Trim(),
                rate,
                avgMs,
                answered,
                noAnswer,
                errors,
                t,
                r.StartedAt));
        }

        versionTrend.Sort((a, b) => CompareVersionStrings(a.Version, b.Version));

        var latestResults = await ctx.TestResults.AsNoTracking()
            .Include(r => r.Question)
            .Where(r => r.TestRunId == latestRun.Id)
            .ToListAsync(ct);

        if (latestResults.Count == 0 && (latestRun.CompletedAt == null || latestRun.TotalQuestions == 0))
            return null;

        double? prevAnswerRate = null;
        int? prevAvgMs = null;
        var currentV = (latestRun.IvyVersion ?? "").Trim();
        var idx = versionTrend.FindIndex(r => string.Equals(r.Version, currentV, StringComparison.OrdinalIgnoreCase));
        if (idx > 0)
        {
            var prev = versionTrend[idx - 1];
            prevAnswerRate = prev.AnswerRate;
            prevAvgMs = (int)prev.AvgMs;
        }
        else
        {
            var prevRun = await ctx.TestRuns.AsNoTracking()
                .Where(r => r.StartedAt < latestRun.StartedAt && r.CompletedAt != null && runIdsWithData.Contains(r.Id))
                .OrderByDescending(r => r.StartedAt)
                .FirstOrDefaultAsync(ct);
            if (prevRun != null)
            {
                var prevResults = await ctx.TestResults.AsNoTracking()
                    .Where(r => r.TestRunId == prevRun.Id)
                    .ToListAsync(ct);
                if (prevRun.TotalQuestions > 0 && prevResults.Count == prevRun.TotalQuestions)
                {
                    var prevAns = prevResults.Count(r => r.IsSuccess);
                    prevAnswerRate = Math.Round(prevAns * 100.0 / prevResults.Count, 1);
                    prevAvgMs = (int)prevResults.Average(r => r.ResponseTimeMs);
                }
                else if (prevRun.TotalQuestions > 0)
                {
                    prevAnswerRate = Math.Round(prevRun.SuccessCount * 100.0 / prevRun.TotalQuestions, 1);
                    prevAvgMs = prevResults.Count > 0 ? (int)prevResults.Average(r => r.ResponseTimeMs) : 0;
                }
            }
        }

        var detail = BuildDashboardData(latestResults, prevAnswerRate, prevAvgMs, latestRun);

        return new DashboardPageModel(
            currentV,
            latestRun.StartedAt,
            detail,
            versionTrend);
    }

    private static DashboardData BuildDashboardData(
        List<TestResultEntity> results,
        double? prevAnswerRate,
        int? prevAvgMs,
        TestRunEntity? run = null)
    {
        var useRunSummary = run != null && run.TotalQuestions > 0
            && (results.Count == 0 || results.Count != run.TotalQuestions);

        int total, answered, noAnswer, errors;
        double answerRate;
        int avgMs;
        if (useRunSummary)
        {
            total = run!.TotalQuestions;
            answered = run.SuccessCount;
            noAnswer = run.NoAnswerCount;
            errors = run.ErrorCount;
            answerRate = total > 0 ? Math.Round(answered * 100.0 / total, 1) : 0;
            avgMs = results.Count > 0 ? (int)results.Average(r => r.ResponseTimeMs) : 0;
        }
        else if (results.Count == 0)
        {
            return new DashboardData(
                0, 0, 0, 0, 0, 0, prevAnswerRate, prevAvgMs,
                [], [], [], []);
        }
        else
        {
            total = results.Count;
            answered = results.Count(r => r.IsSuccess);
            noAnswer = results.Count(r => !r.IsSuccess && r.HttpStatus == 404);
            errors = results.Count(r => !r.IsSuccess && r.HttpStatus != 404);
            answerRate = total > 0 ? Math.Round(answered * 100.0 / total, 1) : 0;
            avgMs = (int)results.Average(r => r.ResponseTimeMs);
        }

        var widgetGroups = results
            .GroupBy(r => r.Question.Widget)
            .Select(g =>
            {
                var t = g.Count();
                var a = g.Count(r => r.IsSuccess);
                var rate = t > 0 ? Math.Round(a * 100.0 / t, 1) : 0;
                var avg = (int)g.Average(r => r.ResponseTimeMs);
                var max = g.Max(r => r.ResponseTimeMs);
                return new WidgetProblem(g.Key, rate, t - a, t, avg, max);
            })
            .ToList();

        var worstWidgets = widgetGroups.OrderBy(w => w.AnswerRate).Take(10).ToList();
        var slowestWidgets = widgetGroups.OrderByDescending(w => w.AvgMs).Take(10).ToList();

        var diffBreakdown = results
            .GroupBy(r => r.Question.Difficulty)
            .Select(g =>
            {
                var t = g.Count();
                var a = g.Count(r => r.IsSuccess);
                var na = g.Count(r => !r.IsSuccess && r.HttpStatus == 404);
                var err = g.Count(r => !r.IsSuccess && r.HttpStatus != 404);
                var rate = t > 0 ? Math.Round(a * 100.0 / t, 1) : 0;
                return new DifficultyRow(g.Key, rate, a, na, err, t);
            })
            .OrderBy(d => d.Difficulty == "easy" ? 0 : d.Difficulty == "medium" ? 1 : 2)
            .ToList();

        var failedQuestions = results
            .Where(r => !r.IsSuccess)
            .OrderBy(r => r.Question.Widget)
            .ThenBy(r => r.Question.Difficulty)
            .Select(r => new FailedQuestion(
                r.Question.Widget,
                r.Question.Difficulty,
                r.Question.QuestionText,
                r.HttpStatus == 404 ? "no answer" : "error",
                r.ResponseTimeMs))
            .ToList();

        return new DashboardData(
            total, answered, noAnswer, errors, answerRate, avgMs,
            prevAnswerRate, prevAvgMs,
            worstWidgets, slowestWidgets, diffBreakdown, failedQuestions);
    }

    /// <summary>Semantic-ish ordering so 1.2.26 &lt; 1.2.27 &lt; 1.10.0.</summary>
    private static int CompareVersionStrings(string? a, string? b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.IsNullOrEmpty(a)) return -1;
        if (string.IsNullOrEmpty(b)) return 1;

        var pa = a.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pb = b.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var n = Math.Max(pa.Length, pb.Length);
        for (var i = 0; i < n; i++)
        {
            var sa = i < pa.Length ? pa[i] : "";
            var sb = i < pb.Length ? pb[i] : "";
            var na = int.TryParse(sa, out var ia) ? ia : int.MinValue;
            var nb = int.TryParse(sb, out var ib) ? ib : int.MinValue;
            if (na != int.MinValue && nb != int.MinValue)
            {
                if (na != nb) return na.CompareTo(nb);
                continue;
            }

            var cmp = string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;
        }

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}

internal record DashboardPageModel(
    string IvyVersion,
    DateTime RunStartedAt,
    DashboardData Detail,
    List<VersionTrendRow> VersionTrend);

internal record VersionTrendRow(
    string Version,
    double AnswerRate,
    double AvgMs,
    int Answered,
    int NoAnswer,
    int Errors,
    int Total,
    DateTime RunAt);

internal record DashboardData(
    int Total, int Answered, int NoAnswer, int Errors,
    double AnswerRate, int AvgMs,
    double? PrevAnswerRate, int? PrevAvgMs,
    List<WidgetProblem> WorstWidgets,
    List<WidgetProblem> SlowestWidgets,
    List<DifficultyRow> DifficultyBreakdown,
    List<FailedQuestion> FailedQuestions);

internal record WidgetProblem(string Widget, double AnswerRate, int Failed, int Tested, int AvgMs, int MaxMs);
internal record DifficultyRow(string Difficulty, double Rate, int Answered, int NoAnswer, int Errors, int Total);
internal record FailedQuestion(string Widget, string Difficulty, string Question, string Status, int ResponseTimeMs);
