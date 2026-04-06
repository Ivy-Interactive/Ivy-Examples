using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace IvyAskStatistics.Apps;

[App(icon: Icons.ChartBar, title: "Run Tests")]
public class RunApp : ViewBase
{
    /// <summary>Invalidate with <see cref="IQueryService.RevalidateByTag"/> when rows in <c>Questions</c> change.</summary>
    internal const string TestQuestionsQueryTag = "test-questions-db";

    internal const string LastSavedRunQueryTag = "last-saved-run";

    /// <summary>
    /// Must differ from other apps' <see cref="UseQuery{TValue,TKey}"/> keys (e.g. Dashboard uses its own string).
    /// Reusing <c>0</c> across tabs overwrote the server query cache and cleared this panel after navigation.
    /// </summary>
    private const string LastSavedRunQueryKey = "run-tests-last-saved-db";

    private static LastSavedRunSummary? s_lastSuccessfulLastSavedRun;

    /// <summary>Fixed parallel Ask requests for every run (not user-configurable).</summary>
    private const int RunParallelism = 20;

    private static readonly string[] DifficultyOptions = ["all", "easy", "medium", "hard"];
    private static readonly string[] McpEnvironmentOptions = ["production", "staging"];

    private static string McpBaseUrl(string environment) =>
        environment.Equals("staging", StringComparison.OrdinalIgnoreCase)
            ? "https://staging.mcp.ivy.app"
            : "https://mcp.ivy.app";

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();
        var configuration = UseService<IConfiguration>();
        var client = UseService<IClientProvider>();
        var queryService = UseService<IQueryService>();

        var ivyVersion = UseState(RunTestFormPreferences.IvyVersion);
        var mcpEnvironment = UseState(RunTestFormPreferences.McpEnvironment);
        var difficultyFilter = UseState(RunTestFormPreferences.DifficultyFilter);
        var isRunning = UseState(false);
        var completed = UseState<ImmutableList<QuestionRun>>(ImmutableList<QuestionRun>.Empty);
        var activeIds = UseState(ImmutableHashSet<string>.Empty);
        var allQuestions = UseState<List<TestQuestion>>([]);
        var persistToDb = UseState(false);
        var refreshToken = UseRefreshToken();
        var runFinished = UseState(false);
        var runVersionExistsDialogOpen = UseState(false);

        UseEffect(() =>
        {
            completed.Set(ImmutableList<QuestionRun>.Empty);
            activeIds.Set(ImmutableHashSet<string>.Empty);
            allQuestions.Set([]);
            runFinished.Set(false);
        }, [difficultyFilter.ToTrigger()]);

        UseEffect(() =>
        {
            ivyVersion.Set(RunTestFormPreferences.IvyVersion);
            mcpEnvironment.Set(RunTestFormPreferences.McpEnvironment);
            difficultyFilter.Set(RunTestFormPreferences.DifficultyFilter);
        }, EffectTrigger.OnMount());

        UseEffect(() =>
        {
            RunTestFormPreferences.Set(ivyVersion.Value, mcpEnvironment.Value, difficultyFilter.Value);
        }, [ivyVersion.ToTrigger(), mcpEnvironment.ToTrigger(), difficultyFilter.ToTrigger()]);

        var questionsQuery = UseQuery<List<TestQuestion>, string>(
            key: $"questions-{difficultyFilter.Value}",
            fetcher: async (_, ct) =>
            {
                var result = await LoadQuestionsAsync(factory, difficultyFilter.Value);
                refreshToken.Refresh();
                return result;
            },
            tags: [TestQuestionsQueryTag],
            options: new QueryOptions { RevalidateOnMount = true, KeepPrevious = true });

        var lastSavedRunQuery = UseQuery<LastSavedRunSummary?, string>(
            key: LastSavedRunQueryKey,
            fetcher: async (_, ct) =>
            {
                try
                {
                    var r = await LoadLastSavedRunAsync(factory, ct);
                    s_lastSuccessfulLastSavedRun = r;
                    refreshToken.Refresh();
                    return r;
                }
                catch (OperationCanceledException)
                {
                    return s_lastSuccessfulLastSavedRun;
                }
            },
            tags: [LastSavedRunQueryTag],
            options: new QueryOptions { KeepPrevious = true, RevalidateOnMount = true });

        // Latest ivy_ask_test_runs.IvyVersion when the input is still empty (prefs / first visit).
        UseEffect(() =>
        {
            if (lastSavedRunQuery.Loading) return;
            var summary = lastSavedRunQuery.Value ?? s_lastSuccessfulLastSavedRun;
            if (summary == null || string.IsNullOrWhiteSpace(summary.IvyVersion)) return;
            if (!string.IsNullOrWhiteSpace(ivyVersion.Value.Trim())) return;
            ivyVersion.Set(summary.IvyVersion.Trim());
        }, EffectTrigger.OnBuild());

        // Do not call Mutator.Revalidate() from EffectTrigger.OnMount here: both queries already use
        // RevalidateOnMount, and an extra OnMount revalidate starts a second fetch that cancels the first,
        // producing OperationCanceledException + Ivy.QueryService "Fetch failed" warnings.

        var running = isRunning.Value;
        var firstLoad = questionsQuery.Loading && questionsQuery.Value == null && !running;
        var questions = running && allQuestions.Value.Count > 0 ? allQuestions.Value : questionsQuery.Value ?? [];
        var completedList = completed.Value;
        var active = activeIds.Value;

        var done = completedList.Count;
        var success = completedList.Count(r => r.Status == "success");
        var noAnswer = completedList.Count(r => r.Status == "no_answer");
        var errors = completedList.Count(r => r.Status == "error");
        var avgMs = done > 0 ? (int)completedList.Average(r => r.ResponseTimeMs) : 0;
        var progressPct = questions.Count > 0 ? done * 100 / questions.Count : 0;

        var lastSavedEffective = lastSavedRunQuery.Value ?? s_lastSuccessfulLastSavedRun;

        List<QuestionRow> rows;
        if (running || done > 0)
        {
            var completedById = completedList.ToDictionary(r => r.Question.Id);
            rows = questions.Select(q =>
            {
                if (completedById.TryGetValue(q.Id, out var r))
                {
                    var icon = r.Status == "success" ? Icons.CircleCheck : Icons.CircleX;
                    return new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, icon, ToStatusLabel(r.Status), $"{r.ResponseTimeMs}ms");
                }

                if (active.Contains(q.Id))
                    return new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, Icons.Loader, "running", "");

                return new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, Icons.Clock, "pending", "");
            }).ToList();
        }
        else if (lastSavedEffective?.Rows.Count > 0)
            rows = BuildQuestionRowsFromLastSaved(lastSavedEffective);
        else
            rows = questions.Select(q =>
                    new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, Icons.Clock, "pending", ""))
                .ToList();

        async Task OnRunAllClickedAsync()
        {
            var snapshot = questionsQuery.Value ?? [];
            if (snapshot.Count == 0) return;

            var version = ivyVersion.Value.Trim();
            if (string.IsNullOrEmpty(version))
            {
                client.Toast("Please enter an Ivy version before running.");
                return;
            }

            if (await RunExistsAsync(factory, version))
            {
                runVersionExistsDialogOpen.Set(true);
                return;
            }

            await BeginRunAsync(persistToDatabase: true, replaceExistingRunForVersion: false);
        }

        async Task BeginRunAsync(bool persistToDatabase, bool replaceExistingRunForVersion)
        {
            var snapshot = questionsQuery.Value ?? [];
            if (snapshot.Count == 0) return;

            var version = ivyVersion.Value.Trim();
            if (string.IsNullOrEmpty(version))
            {
                client.Toast("Please enter an Ivy version before running.");
                return;
            }

            persistToDb.Set(persistToDatabase);

            completed.Set(ImmutableList<QuestionRun>.Empty);
            activeIds.Set(ImmutableHashSet<string>.Empty);
            allQuestions.Set(snapshot);
            runFinished.Set(false);
            isRunning.Set(true);
            refreshToken.Refresh();

            var maxParallel = RunParallelism;
            var baseUrl = McpBaseUrl(mcpEnvironment.Value);
            var mcpClient = IvyAskService.ResolveMcpClientId(configuration);

            _ = Task.Run(async () =>
            {
                var runStartedUtc = DateTime.UtcNow;
                var bag = new ConcurrentBag<QuestionRun>();
                var inFlight = new ConcurrentDictionary<string, bool>();
                using var sem = new SemaphoreSlim(maxParallel);

                using var ticker = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
                var tickerCts = new CancellationTokenSource();
                var uiTask = Task.Run(async () =>
                {
                    while (!tickerCts.IsCancellationRequested)
                    {
                        try { await ticker.WaitForNextTickAsync(tickerCts.Token); } catch { break; }
                        completed.Set(_ => bag.ToImmutableList());
                        activeIds.Set(_ => inFlight.Keys.ToImmutableHashSet());
                        refreshToken.Refresh();
                    }
                });

                var tasks = snapshot.Select(async q =>
                {
                    await sem.WaitAsync();
                    inFlight[q.Id] = true;

                    try
                    {
                        var result = await IvyAskService.AskAsync(q, baseUrl, mcpClient);
                        bag.Add(result);
                    }
                    finally
                    {
                        inFlight.TryRemove(q.Id, out _);
                        sem.Release();
                    }
                });

                await Task.WhenAll(tasks);
                tickerCts.Cancel();
                await uiTask;

                completed.Set(_ => bag.ToImmutableList());
                activeIds.Set(ImmutableHashSet<string>.Empty);

                var finalResults = OrderResultsLikeSnapshot(snapshot, bag.ToList());
                if (persistToDatabase)
                {
                    var saved = await PersistNewRunAsync(
                        factory,
                        version,
                        snapshot,
                        finalResults,
                        runStartedUtc,
                        mcpEnvironment.Value,
                        difficultyFilter.Value,
                        RunParallelism.ToString(),
                        replaceExistingRunForVersion);
                    if (saved)
                    {
                        queryService.RevalidateByTag("dashboard-stats");
                        queryService.RevalidateByTag(LastSavedRunQueryTag);
                    }
                    else
                        client.Toast("Could not save results to the database.");
                }

                isRunning.Set(false);
                runFinished.Set(true);
                refreshToken.Refresh();
            });
        }

        object lastSavedRunPanel = BuildLastSavedRunPanel(lastSavedRunQuery, lastSavedEffective);

        var mcpBaseForUi = McpBaseUrl(mcpEnvironment.Value);

        var controls = Layout.Horizontal().Gap(2).Height(Size.Fit())
            | ivyVersion.ToTextInput().Placeholder("Ivy version (e.g. v2.4.0)").Disabled(running)
            | mcpEnvironment.ToSelectInput(McpEnvironmentOptions).Disabled(running)
            | difficultyFilter.ToSelectInput(DifficultyOptions).Disabled(running)
            | new Button("Run All", onClick: async _ => await OnRunAllClickedAsync())
                .Primary()
                .Icon(Icons.Play)
                .Disabled(running || questionsQuery.Loading || questions.Count == 0);

        if (firstLoad)
            return Layout.Vertical().Height(Size.Full())
                   | controls
                   | lastSavedRunPanel
                   | TabLoadingSkeletons.RunTab();

        object statusBar;
        if (running)
        {
            var inFlight = active.Count;
            statusBar = new Callout(
                Layout.Vertical()
                    | Text.Block(
                        $"Running {done}/{questions.Count} completed, {inFlight} in flight (x{RunParallelism} parallel) · {mcpBaseForUi}")
                    | new Progress(progressPct).Goal($"{done}/{questions.Count}"),
                variant: CalloutVariant.Info);
        }
        else if (runFinished.Value && done > 0)
        {
            var suffix = persistToDb.Value ? "Results saved to database." : "Local only — this version was already tested.";
            var hasErrors = errors > 0;
            statusBar = new Callout(
                Text.Block(hasErrors
                    ? $"Completed: {success}/{done} answered, {noAnswer} no answer, {errors} error(s). {suffix}"
                    : $"Done! {success}/{done} answered, {noAnswer} no answer. {suffix}"),
                variant: hasErrors ? CalloutVariant.Warning : CalloutVariant.Success);
        }
        else
        {
            statusBar = Text.Muted("");
        }

        object kpiCards = done > 0
            ? BuildOutcomeMetricCards(
                done,
                success,
                noAnswer,
                errors,
                avgMs,
                completedList.Min(r => r.ResponseTimeMs),
                completedList.Max(r => r.ResponseTimeMs))
            : Text.Muted("");

        object liveRunKpiWhileRunning = done > 0
            ? BuildOutcomeMetricCards(
                done,
                success,
                noAnswer,
                errors,
                avgMs,
                completedList.Min(r => r.ResponseTimeMs),
                completedList.Max(r => r.ResponseTimeMs))
            : BuildLiveRunMetricPlaceholders(questions.Count);

        // After a finished session, show only this run’s metrics + callout — not the older DB snapshot row.
        object historyOrActiveRunPanel = running
            ? Layout.Vertical().Gap(2)
              | statusBar
              | liveRunKpiWhileRunning
            : runFinished.Value && done > 0
                ? new Empty()
                : lastSavedRunPanel;

        // Completed run: green status + current KPI cards (no "This run" heading; KPIs are not duplicated above).
        object afterHistoryPanel = !running && runFinished.Value && done > 0
            ? Layout.Vertical().Gap(2)
              | statusBar
              | kpiCards
            : new Empty();

        var showingLastSavedSnapshot = !running && done == 0 && lastSavedEffective?.Rows.Count > 0;
        var mainTableKey = showingLastSavedSnapshot ? "run-tests-last-saved" : "run-tests-live";

        var table = rows.AsQueryable()
            .ToDataTable()
            .RefreshToken(refreshToken)
            .Key(mainTableKey)
            .Height(Size.Full())
            .Hidden(r => r.Id)
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Difficulty, "Difficulty")
            .Header(r => r.Question, "Question")
            .Header(r => r.ResultIcon, "Icon")
            .Header(r => r.Status, "Status")
            .Header(r => r.Time, "Time")
            .Width(r => r.ResultIcon, Size.Px(50))
            .AlignContent(r => r.ResultIcon, Align.Center)
            .Width(r => r.Widget, Size.Px(140))
            .Width(r => r.Question, Size.Px(400))
            .Width(r => r.Difficulty, Size.Px(80))
            .Width(r => r.Status, Size.Px(90))
            .Width(r => r.Time, Size.Px(80))
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
                config.ShowIndexColumn = true;
            });


        // Default Vertical gap is 4 (1rem) between every child; avoid empty Text.Muted placeholders that
        // still consume gaps. Tight coupling between the metric/header block and the DataTable: gap 0 here.
        var runPageTop = Layout.Vertical().Gap(2)
            | controls
            | historyOrActiveRunPanel;

        var runPageLayout = Layout.Vertical().Height(Size.Full()).Gap(2)
            | runPageTop
            | afterHistoryPanel
            | table;

        object? versionExistsDialog = runVersionExistsDialogOpen.Value
            ? new Dialog(
                onClose: _ => runVersionExistsDialogOpen.Set(false),
                header: new DialogHeader("This Ivy version is already in the database"),
                body: new DialogBody(
                    Text.Block(
                        $"A completed run for \"{ivyVersion.Value.Trim()}\" is already stored. "
                        + "You can run tests locally without saving, replace the stored run with new results, or cancel.")),
                footer: new DialogFooter(
                    new Button("Cancel")
                        .Variant(ButtonVariant.Outline)
                        .OnClick(_ => runVersionExistsDialogOpen.Set(false)),
                    new Button("Run without saving")
                        .OnClick(async _ =>
                        {
                            runVersionExistsDialogOpen.Set(false);
                            await BeginRunAsync(persistToDatabase: false, replaceExistingRunForVersion: false);
                        }),
                    new Button("Run and replace in database")
                        .Primary()
                        .Icon(Icons.Database)
                        .OnClick(async _ =>
                        {
                            runVersionExistsDialogOpen.Set(false);
                            await BeginRunAsync(persistToDatabase: true, replaceExistingRunForVersion: true);
                        })))
            : null;

        return versionExistsDialog != null
            ? new Fragment(runPageLayout, versionExistsDialog)
            : runPageLayout;
    }

    /// <summary>Four metric cards before the first response returns (zeros / placeholders).</summary>
    private static object BuildLiveRunMetricPlaceholders(int queuedCount)
    {
        var hint = queuedCount > 0 ? $"{queuedCount} in queue" : "Starting…";
        return Layout.Grid().Columns(4).Height(Size.Fit())
               | new Card(
                   Layout.Vertical()
                       | Text.H3("0%")
                       | Text.Block(hint).Muted()
               ).Title("Answer rate").Icon(Icons.CircleCheck)
               | new Card(
                   Layout.Vertical()
                       | Text.H3("0")
                       | Text.Block("no answer").Muted()
               ).Title("No answer").Icon(Icons.Ban)
               | new Card(
                   Layout.Vertical()
                       | Text.H3("0")
                       | Text.Block("failed / error").Muted()
               ).Title("Errors").Icon(Icons.CircleX)
               | new Card(
                   Layout.Vertical()
                       | Text.H3("—")
                       | Text.Block("waiting for timings…").Muted()
               ).Title("Avg response").Icon(Icons.Timer);
    }

    /// <summary>Same four metric cards as after a live “Run All” completes.</summary>
    private static object BuildOutcomeMetricCards(
        int total,
        int success,
        int noAnswer,
        int errors,
        int avgMs,
        int minMs,
        int maxMs)
    {
        if (total <= 0)
            return Text.Muted("");

        var rate = Math.Round(success * 100.0 / total, 1);
        var timingFooter = $"fastest {minMs} ms · slowest {maxMs} ms";

        return Layout.Grid().Columns(4).Height(Size.Fit())
               | new Card(
                   Layout.Vertical()
                       | Text.H3($"{rate}%")
                       | Text.Block($"{success} of {total} answered").Muted()
               ).Title("Answer rate").Icon(Icons.CircleCheck)
               | new Card(
                   Layout.Vertical()
                       | Text.H3($"{noAnswer}")
                       | Text.Block("no answer").Muted()
               ).Title("No answer").Icon(Icons.Ban)
               | new Card(
                   Layout.Vertical()
                       | Text.H3($"{errors}")
                       | Text.Block("failed / error").Muted()
               ).Title("Errors").Icon(Icons.CircleX)
               | new Card(
                   Layout.Vertical()
                       | Text.H3($"{avgMs} ms")
                       | Text.Block(timingFooter).Muted()
               ).Title("Avg response").Icon(Icons.Timer);
    }

    private static object BuildLastSavedRunPanel(
        QueryResult<LastSavedRunSummary?> query,
        LastSavedRunSummary? effective)
    {
        if (query.Loading && effective == null)
        {
            return Layout.Vertical().Gap(2)
                   | Text.Block("Loading last saved results…").Muted()
                   | Layout.Grid().Columns(4).Height(Size.Fit())
                       | new Skeleton().Height(Size.Units(14))
                       | new Skeleton().Height(Size.Units(14))
                       | new Skeleton().Height(Size.Units(14))
                       | new Skeleton().Height(Size.Units(14));
        }

        var s = effective;
        if (s == null)
        {
            return Layout.Vertical().Gap(2)
                   | Text.Block("No saved test run yet.").Muted()
                   | Text.Muted(
                       "The first completed run for each Ivy version is stored in the database. Re-runs for the same version stay in this session only until you refresh.");
        }

        var times = s.Rows.Select(r => r.ResponseTimeMs).ToList();
        var avgSaved = times.Count > 0 ? (int)times.Average() : 0;
        var minSaved = times.Count > 0 ? times.Min() : 0;
        var maxSaved = times.Count > 0 ? times.Max() : 0;
        var totalForMetrics = s.TotalQuestions > 0
            ? s.TotalQuestions
            : Math.Max(s.SuccessCount + s.NoAnswerCount + s.ErrorCount, 1);

        var metricCards = BuildOutcomeMetricCards(
            totalForMetrics,
            s.SuccessCount,
            s.NoAnswerCount,
            s.ErrorCount,
            avgSaved,
            minSaved,
            maxSaved);

        if (s.Rows.Count == 0)
        {
            return Layout.Vertical().Gap(3)
                   | metricCards
                   | Text.Muted("No per-question rows linked to this run.");
        }

        return Layout.Vertical()
               | metricCards;
    }

    private static List<QuestionRow> BuildQuestionRowsFromLastSaved(LastSavedRunSummary s) =>
        s.Rows.Select(
                (r, i) => new QuestionRow(
                    $"last-saved-{i}",
                    r.Widget,
                    r.Difficulty,
                    r.QuestionPreview,
                    r.Outcome == "answered"
                        ? Icons.CircleCheck
                        : r.Outcome == "no answer"
                            ? Icons.Ban
                            : Icons.CircleX,
                    r.Outcome,
                    $"{r.ResponseTimeMs} ms"))
            .ToList();

    private static async Task<List<TestQuestion>> LoadQuestionsAsync(
        AppDbContextFactory factory,
        string difficulty)
    {
        await using var ctx = factory.CreateDbContext();
        var query = ctx.Questions.Where(q => q.IsActive);
        if (difficulty != "all")
            query = query.Where(q => q.Difficulty == difficulty);

        var entities = await query
            .OrderBy(q => q.Widget)
            .ThenBy(q => q.Difficulty)
            .ToListAsync();

        return entities
            .Select(e => new TestQuestion(e.Id.ToString(), e.Widget, e.Difficulty, e.QuestionText))
            .ToList();
    }

    private static async Task<bool> RunExistsAsync(AppDbContextFactory factory, string ivyVersion)
    {
        await using var ctx = factory.CreateDbContext();
        return await ctx.TestRuns.AnyAsync(r => r.IvyVersion == ivyVersion);
    }

    /// <summary>
    /// One row per question in <paramref name="snapshot"/> order (fills gaps if the bag is short).
    /// </summary>
    private static List<QuestionRun> OrderResultsLikeSnapshot(
        IReadOnlyList<TestQuestion> snapshot,
        List<QuestionRun> bag)
    {
        var byId = bag
            .GroupBy(r => r.Question.Id)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        return snapshot
            .Select(q => byId.TryGetValue(q.Id, out var r)
                ? r
                : new QuestionRun(q, "error", 0, 0, ""))
            .ToList();
    }

    /// <summary>
    /// Single transaction: create run + insert every test result, or roll back (no orphan run / partial rows).
    /// </summary>
    private static async Task<bool> PersistNewRunAsync(
        AppDbContextFactory factory,
        string ivyVersion,
        IReadOnlyList<TestQuestion> snapshot,
        List<QuestionRun> ordered,
        DateTime startedAtUtc,
        string mcpEnvironment,
        string difficultyFilter,
        string concurrency,
        bool replaceExistingRunForVersion)
    {
        if (ordered.Count != snapshot.Count)
            return false;

        await using var ctx = factory.CreateDbContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (replaceExistingRunForVersion)
            {
                var oldRuns = await ctx.TestRuns.Where(r => r.IvyVersion == ivyVersion).ToListAsync();
                if (oldRuns.Count > 0)
                {
                    ctx.TestRuns.RemoveRange(oldRuns);
                    await ctx.SaveChangesAsync();
                }
            }

            var run = new TestRunEntity
            {
                IvyVersion       = ivyVersion,
                Environment    = mcpEnvironment.Trim().ToLowerInvariant() is "staging" ? "staging" : "production",
                DifficultyFilter = string.IsNullOrEmpty(difficultyFilter) ? "all" : difficultyFilter,
                Concurrency    = concurrency ?? "",
                TotalQuestions = snapshot.Count,
                StartedAt      = startedAtUtc,
                SuccessCount   = ordered.Count(r => r.Status == "success"),
                NoAnswerCount  = ordered.Count(r => r.Status == "no_answer"),
                ErrorCount     = ordered.Count(r => r.Status == "error"),
                CompletedAt    = DateTime.UtcNow
            };
            ctx.TestRuns.Add(run);

            var rows = new List<TestResultEntity>(ordered.Count);
            foreach (var result in ordered)
            {
                if (!Guid.TryParse(result.Question.Id, out var questionId))
                    throw new InvalidOperationException($"Invalid question id: {result.Question.Id}");

                rows.Add(new TestResultEntity
                {
                    TestRunId = run.Id,
                    QuestionId = questionId,
                    ResponseText = result.AnswerText ?? "",
                    ResponseTimeMs = result.ResponseTimeMs,
                    IsSuccess = result.Status == "success",
                    HttpStatus = result.HttpStatus,
                    ErrorMessage = result.Status == "error" ? result.AnswerText : null
                });
            }

            ctx.TestResults.AddRange(rows);
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    private static string ToStatusLabel(string status) => status switch
    {
        "success" => "answered",
        "no_answer" => "no answer",
        "error" => "error",
        _ => status.Replace('_', ' ')
    };

    private static string PreviewQuestionText(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;
        return text[..maxChars] + "…";
    }

    private static async Task<LastSavedRunSummary?> LoadLastSavedRunAsync(
        AppDbContextFactory factory,
        CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();
        var run = await ctx.TestRuns.AsNoTracking()
            .OrderByDescending(r => r.CompletedAt ?? r.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (run == null)
            return null;

        var rows = await (
                from tr in ctx.TestResults.AsNoTracking()
                join q in ctx.Questions.AsNoTracking() on tr.QuestionId equals q.Id
                where tr.TestRunId == run.Id
                orderby q.Widget, q.Difficulty, q.Id
                select new LastSavedRunResultRow(
                    q.Widget,
                    q.Difficulty,
                    PreviewQuestionText(q.QuestionText ?? "", 120),
                    tr.IsSuccess
                        ? "answered"
                        : tr.HttpStatus == 404
                            ? "no answer"
                            : "error",
                    tr.ResponseTimeMs))
            .ToListAsync(ct);

        return new LastSavedRunSummary(
            run.Id,
            run.IvyVersion,
            run.Environment,
            string.IsNullOrEmpty(run.DifficultyFilter) ? "all" : run.DifficultyFilter,
            run.Concurrency ?? "",
            run.TotalQuestions,
            run.SuccessCount,
            run.NoAnswerCount,
            run.ErrorCount,
            run.StartedAt,
            run.CompletedAt,
            rows);
    }
}

/// <summary>
/// Remembers Run form fields for the current server process so tab switches do not reset MCP / difficulty / version.
/// </summary>
file static class RunTestFormPreferences
{
    public static string IvyVersion { get; private set; } = "";

    public static string McpEnvironment { get; private set; } = "production";

    public static string DifficultyFilter { get; private set; } = "all";

    public static void Set(string ivy, string mcp, string diff)
    {
        IvyVersion = ivy ?? "";
        McpEnvironment = string.IsNullOrEmpty(mcp) ? "production" : mcp;
        DifficultyFilter = string.IsNullOrEmpty(diff) ? "all" : diff;
    }
}
