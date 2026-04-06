namespace IvyAskStatistics.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    /// <summary>
    /// Survives tab switches: <see cref="UseQuery"/> state is recreated when the view remounts,
    /// but we still need the last successful payload so a failed refetch does not wipe the UI.
    /// Use a unique string query key (not shared <c>0</c> with other apps) so the server cache is not clobbered.
    /// </summary>
    private static readonly Dictionary<string, DashboardPageModel?> s_lastDashboardByKey = new(StringComparer.Ordinal);

    public override object? Build()
    {
        var factory        = UseService<AppDbContextFactory>();
        var client         = UseService<IClientProvider>();
        var navigation     = Context.UseNavigation();
        var selectedRunId  = UseState<Guid?>(null);
        var runDialogOpen  = UseState(false);
        var editSheetOpen     = UseState(false);
        var editQuestionId    = UseState(Guid.Empty);
        var editPreviewResultId = UseState<Guid?>(null);
        var envOverride    = UseState("production");
        var dashboardFocusVersion = UseState<string?>(null);
        var versionSheetOpen      = UseState(false);

        var dashQuery = UseQuery<DashboardPageModel?, string>(
            key: DashboardQueryKey(dashboardFocusVersion.Value),
            fetcher: async (key, ct) =>
            {
                var focusVersion = ParseDashboardFocusFromQueryKey(key);
                try
                {
                    var r = await LoadDashboardPageAsync(factory, focusVersion, ct);
                    s_lastDashboardByKey[key] = r;
                    return r;
                }
                catch (OperationCanceledException)
                {
                    return s_lastDashboardByKey.TryGetValue(key, out var prev) ? prev : null;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    client.Toast($"Could not load dashboard: {ex.Message}");
                    return s_lastDashboardByKey.TryGetValue(key, out var prev) ? prev : null;
                }
            },
            options: new QueryOptions { KeepPrevious = false },
            tags: ["dashboard-stats"]);

        var dashQueryKey = DashboardQueryKey(dashboardFocusVersion.Value);

        // Prefer live query value; fall back to last success for this query key when remounting or on transient errors.
        var page = dashQuery.Value ?? (s_lastDashboardByKey.TryGetValue(dashQueryKey, out var cached) ? cached : null);

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
        var versionCompare = page.VersionCompare;

        // Resolve data for each env from what the loader returned.
        var prodData = string.Equals(page.PrimaryEnvironment, "production", StringComparison.OrdinalIgnoreCase)
            ? page.Detail : page.PeerDetail;
        var stgData = string.Equals(page.PrimaryEnvironment, "staging", StringComparison.OrdinalIgnoreCase)
            ? page.Detail : page.PeerDetail;
        var hasStagingData    = stgData  != null;
        var hasProductionData = prodData != null;

        // Respect the user's toggle; fall back gracefully when an env has no data.
        var showEnv = envOverride.Value;
        if (showEnv == "staging"    && !hasStagingData)    showEnv = "production";
        if (showEnv == "production" && !hasProductionData) showEnv = "staging";

        DashboardData data;
        DashboardData? peer;
        string envPrimary;
        if (showEnv == "staging" && stgData != null)
        {
            data      = stgData;
            peer      = prodData;
            envPrimary = "Staging";
        }
        else
        {
            data      = prodData ?? page.Detail;
            peer      = stgData;
            envPrimary = "Production";
        }
        var hasPeerCompare = peer != null;

        // Remount KPIs + charts when the selected Ivy version or env slice changes. Keep this stable (no live metrics)
        // so we do not get a new key every build — that would remount widgets and retrigger CSS animations constantly.
        var dashboardVisualKey = $"{dashQueryKey}|{envPrimary}";

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

        var kpiRow = Layout.Grid().Columns(5).Height(Size.Fit()).Key(dashboardVisualKey + "|kpi")
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
            ).Title("Ivy version").Icon(Icons.Tag)
             .OnClick(versionCompare.Count > 0
                 ? _ => versionSheetOpen.Set(true)
                 : _ => { });

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

            versionChartsRow = Layout.Grid().Columns(3).Height(Size.Fit()).Key(dashboardVisualKey + "|vercmp")
                | new Card(rateByVersion).Title("Success rate · production vs staging").Height(Size.Units(70))
                | new Card(latencyByVersion).Title("Avg response · production vs staging").Height(Size.Units(70))
                | new Card(outcomesByVersion).Title("Outcomes · production vs staging").Height(Size.Units(70));
        }
        else
        {
            versionChartsRow = Layout.Vertical();
        }

        var worstChart = data.WorstWidgets.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Answer rate %", x => x.Sum(f => f.AnswerRate));

        var latencyByWidgetChart = data.WorstWidgets.ToBarChart()
            .Dimension("Widget", x => x.Widget)
            .Measure("Avg ms", x => x.Sum(f => (double)f.AvgMs))
            .Measure("Max ms", x => x.Sum(f => (double)f.MaxMs));

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

        // Row 1: 3 charts (prod vs staging by version — always both envs)
        // Row 2: 4 charts (per-selected-env diagnostics)
        var chartsRow = Layout.Grid().Columns(4).Height(Size.Fit()).Key(dashboardVisualKey + "|detail-charts")
            | new Card(worstChart).Title($"Worst widgets — rate % ({envPrimary})").Height(Size.Units(70))
            | new Card(latencyByWidgetChart).Title($"Latency by widget — avg / max ({envPrimary})").Height(Size.Units(70))
            | new Card(difficultyChart).Title($"Results by difficulty ({envPrimary})").Height(Size.Units(70))
            | new Card(pieChart).Title($"Result mix ({envPrimary})").Height(Size.Units(70));

        // ── Test runs table (full width) ──
        var runsTable = page.AllRuns.AsQueryable()
            .ToDataTable(r => r.Id)
            .Height(Size.Units(120))
            .Key($"all-test-runs|{dashQueryKey}")
            .Header(r => r.IvyVersion,      "Ivy version")
            .Header(r => r.Environment,     "Environment")
            .Header(r => r.DifficultyFilter, "Difficulty")
            .Header(r => r.TotalQuestions,  "Total")
            .Header(r => r.SuccessCount,    "Answered")
            .Header(r => r.NoAnswerCount,   "No answer")
            .Header(r => r.ErrorCount,      "Errors")
            .Header(r => r.AnswerRate,      "Rate %")
            .Header(r => r.AvgMs,           "Avg ms")
            .Header(r => r.StartedAt,       "Started")
            .Header(r => r.CompletedAt,     "Completed")
            .Width(r => r.IvyVersion,       Size.Px(120))
            .Width(r => r.Environment,      Size.Px(100))
            .Width(r => r.DifficultyFilter, Size.Px(80))
            .Width(r => r.TotalQuestions,   Size.Px(60))
            .Width(r => r.SuccessCount,     Size.Px(80))
            .Width(r => r.NoAnswerCount,    Size.Px(80))
            .Width(r => r.ErrorCount,       Size.Px(60))
            .Width(r => r.AnswerRate,       Size.Px(70))
            .Width(r => r.AvgMs,            Size.Px(70))
            .Width(r => r.StartedAt,        Size.Px(160))
            .Width(r => r.CompletedAt,      Size.Px(160))
            .Hidden(r => r.Id)
            .RowActions(
                MenuItem.Default(Icons.Eye, "view").Label("View results").Tag("view"))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args?.Tag?.ToString() == "view" && Guid.TryParse(args?.Id?.ToString(), out var id))
                {
                    selectedRunId.Set(id);
                    runDialogOpen.Set(true);
                }
                return ValueTask.CompletedTask;
            })
            .Config(c =>
            {
                c.AllowSorting    = true;
                c.AllowFiltering  = true;
                c.ShowSearch      = true;
                c.ShowIndexColumn = false;
            });
        var tableRuns = Layout.Vertical() | new Card(runsTable).Title("Test runs");

        var mainLayout = Layout.Vertical().Height(Size.Full())
               | kpiRow
               | versionChartsRow
               | chartsRow
               | tableRuns ;

        return new Fragment(
            mainLayout,
            versionSheetOpen.Value && versionCompare.Count > 0
                ? new DashboardVersionPickerSheet(
                    versionSheetOpen,
                    dashboardFocusVersion,
                    versionCompare,
                    (page.IvyVersion ?? "").Trim())
                : new Empty(),
            runDialogOpen.Value && selectedRunId.Value.HasValue
                ? new TestRunResultsDialog(
                    runDialogOpen,
                    selectedRunId.Value.Value,
                    editSheetOpen,
                    editQuestionId,
                    editPreviewResultId)
                : new Empty(),
            editSheetOpen.Value
                ? new QuestionEditSheet(editSheetOpen, editQuestionId.Value, editPreviewResultId)
                : new Empty());
    }

    private const char DashboardQueryKeySep = '\u001f';

    private static string DashboardQueryKey(string? ivyVersionFocus) =>
        "dashboard-stats-page" + DashboardQueryKeySep + (ivyVersionFocus ?? "");

    private static string? ParseDashboardFocusFromQueryKey(string key)
    {
        var i = key.IndexOf(DashboardQueryKeySep);
        if (i < 0 || i >= key.Length - 1)
            return null;
        var tail = key[(i + 1)..].Trim();
        return string.IsNullOrEmpty(tail) ? null : tail;
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
        AppDbContextFactory factory, string? ivyVersionFocus, CancellationToken ct)
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

        var latestProd = runs.FirstOrDefault(r => NormalizeEnvironment(r.Environment) == "production");
        var latestStag = runs.FirstOrDefault(r => NormalizeEnvironment(r.Environment) == "staging");

        TestRunEntity primaryRun;
        string primaryEnv;
        var focus = (ivyVersionFocus ?? "").Trim();
        if (string.IsNullOrEmpty(focus))
        {
            primaryRun = latestProd ?? latestStag ?? runs[0];
            primaryEnv = NormalizeEnvironment(primaryRun.Environment);
        }
        else
        {
            latestProdByVersion.TryGetValue(focus, out var prFocus);
            latestStagByVersion.TryGetValue(focus, out var srFocus);
            if (prFocus != null)
            {
                primaryRun = prFocus;
                primaryEnv = "production";
            }
            else if (srFocus != null)
            {
                primaryRun = srFocus;
                primaryEnv = "staging";
            }
            else
            {
                primaryRun = latestProd ?? latestStag ?? runs[0];
                primaryEnv = NormalizeEnvironment(primaryRun.Environment);
            }
        }

        var allVersions = latestProdByVersion.Keys
            .Union(latestStagByVersion.Keys, StringComparer.OrdinalIgnoreCase)
            .ToList();
        allVersions.Sort(CompareVersionStrings);

        var avgMsByRunId = runIdsWithData.Count == 0
            ? new Dictionary<Guid, double>()
            : await ctx.TestResults.AsNoTracking()
                .Where(r => runIdsWithData.Contains(r.TestRunId))
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
                // Cannot call NormalizeEnvironment inside IQueryable — EF cannot translate it to SQL.
                var prevRunBase = ctx.TestRuns.AsNoTracking()
                    .Where(r =>
                        r.StartedAt < primaryRun.StartedAt
                        && r.CompletedAt != null
                        && runIdsWithData.Contains(r.Id));
                var prevRunQuery = string.Equals(primaryEnv, "staging", StringComparison.Ordinal)
                    ? prevRunBase.Where(r => (r.Environment ?? "").Trim().ToLower() == "staging")
                    : prevRunBase.Where(r => (r.Environment ?? "").Trim().ToLower() != "staging");
                var prevRun = await prevRunQuery
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

        var allRuns = runs.Select(r =>
        {
            var t    = r.TotalQuestions;
            var rate = t > 0 ? Math.Round(r.SuccessCount * 100.0 / t, 1) : 0.0;
            var avg  = avgMsByRunId.TryGetValue(r.Id, out var a) ? (int)Math.Round(a) : 0;
            return new TestRunRow(
                r.Id,
                r.IvyVersion ?? "",
                CapitalizeEnv(r.Environment ?? "production"),
                r.DifficultyFilter ?? "all",
                r.TotalQuestions,
                r.SuccessCount,
                r.NoAnswerCount,
                r.ErrorCount,
                rate,
                avg,
                r.StartedAt.ToLocalTime(),
                r.CompletedAt?.ToLocalTime());
        }).ToList();

        return new DashboardPageModel(
            currentV,
            primaryRun.StartedAt,
            primaryEnv,
            detail,
            peerDetail,
            peerEnv,
            versionCompare,
            allRuns);
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
                [], []);
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

        return new DashboardData(
            total, answered, noAnswer, errors, answerRate, avgMs,
            prevAnswerRate, prevAvgMs,
            worstWidgets, diffBreakdown);
    }

    /// <summary>Semantic-ish ordering so 1.2.26 &lt; 1.2.27 &lt; 1.10.0.</summary>
    internal static int CompareVersionStrings(string? a, string? b)
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

internal sealed class DashboardVersionPickerSheet(
    IState<bool> isOpen,
    IState<string?> dashboardFocusVersion,
    IReadOnlyList<VersionCompareRow> rows,
    string currentDisplayedVersion) : ViewBase
{
    private static readonly Comparer<string> VersionComparerDescending =
        Comparer<string>.Create((a, b) => DashboardApp.CompareVersionStrings(a, b));

    /// <summary>One headline success rate: average when both envs have data, otherwise the single non-zero side.</summary>
    private static string FormatCombinedAnswerRate(VersionCompareRow row)
    {
        var p = row.ProductionAnswerRate;
        var s = row.StagingAnswerRate;
        if (p <= 0 && s <= 0) return "—";
        if (s <= 0) return $"{p:F1}%";
        if (p <= 0) return $"{s:F1}%";
        return $"{(p + s) / 2.0:F1}%";
    }

    public override object? Build()
    {
        var versionSearch = UseState("");

        if (!isOpen.Value)
            return null;

        var q = versionSearch.Value.Trim();
        var filteredRows = (string.IsNullOrEmpty(q)
                ? rows
                : rows.Where(r => r.Version.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.Version, VersionComparerDescending)
            .ToList();

        var listItems = new List<ListItem>();
        foreach (var row in filteredRows)
        {
            var v = row.Version;
            var isCurrent = string.Equals(v, currentDisplayedVersion, StringComparison.OrdinalIgnoreCase);
            var title = isCurrent ? $"{v} (current)" : v;
            listItems.Add(new ListItem(
                title: title,
                subtitle: FormatCombinedAnswerRate(row),
                onClick: () =>
                {
                    dashboardFocusVersion.Set(v);
                    isOpen.Set(false);
                }));
        }

        var sheetDescription = string.IsNullOrEmpty(q)
            ? $"{rows.Count} version(s) with test data"
            : $"{filteredRows.Count} matching · {rows.Count} total";

        // Sheet body is a fixed column: intro + search stay put; only the list scrolls (flex child with Grow).
        var body = Layout.Vertical().Height(Size.Full())
            | versionSearch.ToSearchInput().Placeholder("Search versions…").Width(Size.Full())
            | (!string.IsNullOrEmpty(q) && filteredRows.Count == 0
                ? (object)Text.Block("No versions match your search — try another query or clear the filter.").Muted()
                : new Empty())
            | new List(listItems.ToArray()).Height(Size.Full());

        return new Sheet(
                _ => isOpen.Set(false),
                body,
                title: "Ivy version",
                description: sheetDescription)
            .Width(Size.Fraction(0.28f))
            .Height(Size.Full());
    }
}

internal record DashboardPageModel(
    string IvyVersion,
    DateTime RunStartedAt,
    string PrimaryEnvironment,
    DashboardData Detail,
    DashboardData? PeerDetail,
    string? PeerEnvironment,
    List<VersionCompareRow> VersionCompare,
    List<TestRunRow> AllRuns);

internal record TestRunRow(
    Guid Id,
    string IvyVersion,
    string Environment,
    string DifficultyFilter,
    int TotalQuestions,
    int SuccessCount,
    int NoAnswerCount,
    int ErrorCount,
    double AnswerRate,
    int AvgMs,
    DateTime StartedAt,
    DateTime? CompletedAt);

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
    List<DifficultyRow> DifficultyBreakdown);

internal record WidgetProblem(string Widget, double AnswerRate, int Failed, int Tested, int AvgMs, int MaxMs);
internal record DifficultyRow(string Difficulty, double Rate, int Answered, int NoAnswer, int Errors, int Total);
