namespace IvyAskStatistics.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    /// <summary>
    /// Survives tab switches: <see cref="UseQuery"/> state is recreated when the view remounts,
    /// but we still need the last successful payload so a failed refetch does not wipe the UI.
    /// Use a unique string query key (not shared <c>0</c> with other apps) so the server cache is not clobbered.
    /// </summary>
    private static DashboardPageModel? s_lastSuccessfulDashboard;

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();
        var client = UseService<IClientProvider>();
        var navigation = Context.UseNavigation();

        var dashQuery = UseQuery<DashboardPageModel?, string>(
            key: "dashboard-stats-page",
            fetcher: async (_, ct) =>
            {
                try
                {
                    var r = await LoadDashboardPageAsync(factory, ct);
                    s_lastSuccessfulDashboard = r;
                    return r;
                }
                catch (OperationCanceledException)
                {
                    return s_lastSuccessfulDashboard;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    client.Toast($"Could not load dashboard: {ex.Message}");
                    return s_lastSuccessfulDashboard;
                }
            },
            options: new QueryOptions { KeepPrevious = true },
            tags: ["dashboard-stats"]);

        // Prefer live query value; fall back to last success when remounting or on transient errors.
        var page = dashQuery.Value ?? s_lastSuccessfulDashboard;

        if (dashQuery.Loading && page == null)
            return TabLoadingSkeletons.Dashboard();

        if (page == null)
            return Layout.Vertical().Height(Size.Full()).AlignContent(Align.Center)
                   | new Icon(Icons.LayoutDashboard)
                   | Text.H3("No statistics yet")
                   | Text.Block("No completed test runs found. Run a test to see dashboard statistics.")
                       .Muted()
                   | new Button("Run Tests", onClick: _ => navigation.Navigate(typeof(RunApp)))
                       .Primary()
                       .Icon(Icons.Play);
        var data = page.Detail;
        var peer = page.PeerDetail;
        var versionCompare = page.VersionCompare;
        var envPrimary = CapitalizeEnv(page.PrimaryEnvironment);
        var hasPeerCompare = peer != null && page.PeerEnvironment != null;

        // ── Level 1: KPIs (IvyInsights-style headline + delta vs other env or vs previous version) ──
        var rateStr = $"{data.AnswerRate:F1}%";
        object rateDelta = hasPeerCompare
            ? FormatDeltaWithTrend(data.AnswerRate - peer!.AnswerRate, "%", higherIsBetter: true)
            : data.PrevAnswerRate.HasValue
                ? FormatDeltaWithTrend(data.AnswerRate - data.PrevAnswerRate.Value, "%", higherIsBetter: true)
                : Text.Muted("no baseline");

        // NBSP keeps "181" and "ms" on one line; narrow / Fit columns otherwise wrap between tokens.
        var avgMsStr = $"{data.AvgMs}\u00A0ms";
        object avgMsDelta = hasPeerCompare
            ? FormatDeltaWithTrend(data.AvgMs - peer!.AvgMs, "ms", higherIsBetter: false)
            : data.PrevAvgMs.HasValue
                ? FormatDeltaWithTrend(data.AvgMs - data.PrevAvgMs.Value, "ms", higherIsBetter: false)
                : Text.Muted("no baseline");

        var failedCount = data.NoAnswer + data.Errors;
        var peerFailed = hasPeerCompare ? peer!.NoAnswer + peer.Errors : (int?)null;
        object failedDelta = hasPeerCompare && peerFailed.HasValue
            ? FormatDeltaWithTrend(failedCount - peerFailed.Value, "", higherIsBetter: false, countMode: true)
            : new Empty();

        var runVersion = string.IsNullOrWhiteSpace(page.IvyVersion) ? "—" : page.IvyVersion.Trim();

        var kpiRow = Layout.Grid().Columns(5).Height(Size.Fit())
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | (Layout.Horizontal().AlignContent(Align.Center).Gap(1)
                        | Text.H2(rateStr).Bold()
                        | rateDelta)
            ).Title("Answer success").Icon(Icons.CircleCheck)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | (Layout.Horizontal().AlignContent(Align.Center).Gap(1)
                        | Text.H2(avgMsStr).Bold()
                        | avgMsDelta)
            ).Title("Avg latency").Icon(Icons.Timer)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | (Layout.Horizontal().AlignContent(Align.Center).Gap(1)
                        | Text.H2(failedCount.ToString("N0")).Bold()
                        | failedDelta)
            ).Title("No answer + errors").Icon(Icons.CircleX)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(data.WorstWidgets.Count > 0 ? data.WorstWidgets[0].Widget : "—").Bold()
            ).Title("Weakest widget").Icon(Icons.Ban)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(runVersion).Bold()
            ).Title("Ivy version").Icon(Icons.Tag);

        // ── Production vs staging by Ivy version ──
        object versionChartsRow;
        if (versionCompare.Count >= 1)
        {
            var rateByVersion = versionCompare.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Production %", x => x.Sum(f => f.ProductionAnswerRate))
                .Measure("Staging %", x => x.Sum(f => f.StagingAnswerRate));

            var latencyByVersion = versionCompare.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Production ms", x => x.Sum(f => f.ProductionAvgMs))
                .Measure("Staging ms", x => x.Sum(f => f.StagingAvgMs));

            var outcomesByVersion = versionCompare.ToBarChart()
                .Dimension("Version", x => x.Version)
                .Measure("Prod answered", x => x.Sum(f => f.ProductionAnswered))
                .Measure("Stg answered", x => x.Sum(f => f.StagingAnswered))
                .Measure("Prod no answer", x => x.Sum(f => f.ProductionNoAnswer))
                .Measure("Stg no answer", x => x.Sum(f => f.StagingNoAnswer))
                .Measure("Prod error", x => x.Sum(f => f.ProductionErrors))
                .Measure("Stg error", x => x.Sum(f => f.StagingErrors));

            versionChartsRow = Layout.Grid().Columns(3).Height(Size.Fit())
                | new Card(rateByVersion).Title("Success rate · production vs staging").Height(Size.Units(70))
                | new Card(latencyByVersion).Title("Avg response · production vs staging").Height(Size.Units(70))
                | new Card(outcomesByVersion).Title("Outcomes · production vs staging").Height(Size.Units(70));
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

        var chartsRow = Layout.Grid().Columns(3).Height(Size.Fit())
            | new Card(worstChart).Title($"Worst widgets ({envPrimary})").Height(Size.Units(70))
            | new Card(difficultyChart).Title($"Results by difficulty ({envPrimary})").Height(Size.Units(70))
            | new Card(pieChart).Title($"Result mix ({envPrimary})").Height(Size.Units(70));

        var problemRow = Layout.Grid().Columns(2).Height(Size.Fit())
            | new Card(worstTable).Title($"Worst widgets — top 10 ({envPrimary})")
            | new Card(slowestTable).Title($"Slowest widgets — top 10 ({envPrimary})");

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

        return Layout.Vertical().Height(Size.Full())
               | kpiRow
               | versionChartsRow
               | chartsRow
               | problemRow
               | new Card(failedTable).Title($"Failed questions ({failedCount}) · {envPrimary}");
    }

    private static string CapitalizeEnv(string env) =>
        env.Equals("staging", StringComparison.OrdinalIgnoreCase) ? "Staging" : "Production";

    private static string NormalizeEnvironment(string? environment)
    {
        var e = (environment ?? "").Trim().ToLowerInvariant();
        return e == "staging" ? "staging" : "production";
    }

    /// <summary>Trend icon + colored delta (same idea as IvyInsights KPI cards).</summary>
    private static object FormatDeltaWithTrend(double delta, string unit, bool higherIsBetter, bool countMode = false)
    {
        if (countMode)
        {
            if (delta == 0) return Text.Muted("—");
        }
        else if (unit == "%")
        {
            if (Math.Abs(delta) < 0.05) return Text.Muted("—");
        }
        else if (Math.Abs(delta) < 1) return Text.Muted("—");

        var sign = delta > 0 ? "+" : "";
        var label = countMode
            ? $"{sign}{(int)delta}"
            : unit == "%"
                ? $"{sign}{delta:F1}{unit}"
                : $"{sign}{(int)delta} {unit}";
        var isGood = higherIsBetter ? delta > 0 : delta < 0;
        var icon = isGood ? Icons.TrendingUp : Icons.TrendingDown;
        var color = isGood ? Colors.Success : Colors.Destructive;
        return Layout.Horizontal().Gap(1).AlignContent(Align.Center)
            | new Icon(icon).Color(color)
            | Text.H3(label).Color(color);
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

        var latestProd = runs.FirstOrDefault(r => NormalizeEnvironment(r.Environment) == "production");
        var latestStag = runs.FirstOrDefault(r => NormalizeEnvironment(r.Environment) == "staging");
        var primaryRun = latestProd ?? latestStag ?? runs[0];
        var primaryEnv = NormalizeEnvironment(primaryRun.Environment);

        var latestProdByVersion = new Dictionary<string, TestRunEntity>(StringComparer.OrdinalIgnoreCase);
        var latestStagByVersion = new Dictionary<string, TestRunEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in runs)
        {
            var v = (r.IvyVersion ?? "").Trim();
            if (string.IsNullOrEmpty(v)) continue;
            var dict = NormalizeEnvironment(r.Environment) == "staging" ? latestStagByVersion : latestProdByVersion;
            if (!dict.ContainsKey(v))
                dict[v] = r;
        }

        var allVersions = latestProdByVersion.Keys
            .Union(latestStagByVersion.Keys, StringComparer.OrdinalIgnoreCase)
            .ToList();
        allVersions.Sort(CompareVersionStrings);

        var compareRunIds = allVersions
            .SelectMany(v => new[]
            {
                latestProdByVersion.TryGetValue(v, out var p) ? p.Id : (Guid?)null,
                latestStagByVersion.TryGetValue(v, out var s) ? s.Id : (Guid?)null
            })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var avgMsByRunId = compareRunIds.Count == 0
            ? new Dictionary<Guid, double>()
            : await ctx.TestResults.AsNoTracking()
                .Where(r => compareRunIds.Contains(r.TestRunId))
                .GroupBy(r => r.TestRunId)
                .Select(g => new { g.Key, Avg = g.Average(x => (double)x.ResponseTimeMs) })
                .ToDictionaryAsync(x => x.Key, x => Math.Round(x.Avg, 0), ct);

        static void FillMetrics(
            TestRunEntity? run,
            IReadOnlyDictionary<Guid, double> avgByRun,
            out double rate,
            out double avgMs,
            out int answered,
            out int noAnswer,
            out int errors)
        {
            if (run == null)
            {
                rate = 0;
                avgMs = 0;
                answered = 0;
                noAnswer = 0;
                errors = 0;
                return;
            }

            var t = run.TotalQuestions;
            answered = run.SuccessCount;
            noAnswer = run.NoAnswerCount;
            errors = run.ErrorCount;
            rate = t > 0 ? Math.Round(answered * 100.0 / t, 1) : 0;
            avgMs = avgByRun.TryGetValue(run.Id, out var a) ? a : 0;
        }

        var versionCompare = new List<VersionCompareRow>();
        foreach (var v in allVersions)
        {
            latestProdByVersion.TryGetValue(v, out var pr);
            latestStagByVersion.TryGetValue(v, out var sr);
            FillMetrics(pr, avgMsByRunId, out var pRate, out var pAvg, out var pAns, out var pNa, out var pErr);
            FillMetrics(sr, avgMsByRunId, out var sRate, out var sAvg, out var sAns, out var sNa, out var sErr);
            versionCompare.Add(new VersionCompareRow(
                v, pRate, sRate, pAvg, sAvg, pAns, sAns, pNa, sNa, pErr, sErr));
        }

        var currentV = (primaryRun.IvyVersion ?? "").Trim();
        latestStagByVersion.TryGetValue(currentV, out var stagingForVersion);
        latestProdByVersion.TryGetValue(currentV, out var productionForVersion);

        TestRunEntity? peerRun = null;
        string? peerEnv = null;
        if (primaryEnv == "production" && stagingForVersion != null)
        {
            peerRun = stagingForVersion;
            peerEnv = "staging";
        }
        else if (primaryEnv == "staging" && productionForVersion != null)
        {
            peerRun = productionForVersion;
            peerEnv = "production";
        }

        var trendDict = primaryEnv == "staging" ? latestStagByVersion : latestProdByVersion;
        var versionTrendPrimary = trendDict.Values
            .Select(r =>
            {
                var t = r.TotalQuestions;
                var ans = r.SuccessCount;
                var rate = t > 0 ? Math.Round(ans * 100.0 / t, 1) : 0.0;
                var avg = avgMsByRunId.TryGetValue(r.Id, out var a) ? (int)Math.Round(a) : 0;
                return (Version: (r.IvyVersion ?? "").Trim(), rate, avgMs: avg);
            })
            .OrderBy(x => x.Version, Comparer<string>.Create((a, b) => CompareVersionStrings(a, b)))
            .ToList();

        var latestResults = await ctx.TestResults.AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Question)
            .Where(r => r.TestRunId == primaryRun.Id)
            .ToListAsync(ct);

        if (latestResults.Count == 0 && (primaryRun.CompletedAt == null || primaryRun.TotalQuestions == 0))
            return null;

        double? prevAnswerRate = null;
        int? prevAvgMs = null;
        if (peerRun == null)
        {
            var idx = versionTrendPrimary.FindIndex(r =>
                string.Equals(r.Version, currentV, StringComparison.OrdinalIgnoreCase));
            if (idx > 0)
            {
                var prev = versionTrendPrimary[idx - 1];
                prevAnswerRate = prev.rate;
                prevAvgMs = prev.avgMs;
            }
            else
            {
                var prevRun = await ctx.TestRuns.AsNoTracking()
                    .Where(r =>
                        r.StartedAt < primaryRun.StartedAt
                        && r.CompletedAt != null
                        && runIdsWithData.Contains(r.Id)
                        && NormalizeEnvironment(r.Environment) == primaryEnv)
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
        }

        var detail = BuildDashboardData(latestResults, prevAnswerRate, prevAvgMs, primaryRun);

        DashboardData? peerDetail = null;
        if (peerRun != null)
        {
            var peerResults = await ctx.TestResults.AsNoTracking()
                .AsSplitQuery()
                .Include(r => r.Question)
                .Where(r => r.TestRunId == peerRun.Id)
                .ToListAsync(ct);
            peerDetail = BuildDashboardData(peerResults, null, null, peerRun);
        }

        return new DashboardPageModel(
            currentV,
            primaryRun.StartedAt,
            primaryEnv,
            detail,
            peerDetail,
            peerEnv,
            versionCompare);
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
    string PrimaryEnvironment,
    DashboardData Detail,
    DashboardData? PeerDetail,
    string? PeerEnvironment,
    List<VersionCompareRow> VersionCompare);

/// <summary>Per Ivy version: latest production vs latest staging completed runs (0 when an env has no run).</summary>
internal record VersionCompareRow(
    string Version,
    double ProductionAnswerRate,
    double StagingAnswerRate,
    double ProductionAvgMs,
    double StagingAvgMs,
    int ProductionAnswered,
    int StagingAnswered,
    int ProductionNoAnswer,
    int StagingNoAnswer,
    int ProductionErrors,
    int StagingErrors);

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
