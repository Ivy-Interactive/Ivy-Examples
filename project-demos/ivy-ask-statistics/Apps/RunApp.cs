namespace IvyAskStatistics.Apps;

[App(icon: Icons.ChartBar, title: "Run Tests")]
public class RunApp : ViewBase
{
    private static readonly string[] DifficultyOptions = ["all", "easy", "medium", "hard"];
    private const string BaseUrl = "https://mcp.ivy.app";

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();
        var client = UseService<IClientProvider>();

        var ivyVersion = UseState("");
        var difficultyFilter = UseState("all");
        var runningIndex = UseState(-1);
        var completed = UseState<List<QuestionRun>>([]);
        var runQueue = UseState<List<TestQuestion>>([]);
        var persistToDb = UseState(false);
        var activeRunId = UseState(Guid.Empty);
        var refreshToken = UseRefreshToken();
        var runFinished = UseState(false);

        UseEffect(() =>
        {
            completed.Set([]);
            runningIndex.Set(-1);
            runQueue.Set([]);
            runFinished.Set(false);
        }, [difficultyFilter.ToTrigger()]);

        UseEffect(async () =>
        {
            var idx = runningIndex.Value;
            if (idx < 0) return;

            var questions = runQueue.Value;

            if (idx >= questions.Count)
            {
                if (persistToDb.Value && activeRunId.Value != Guid.Empty)
                    await FinalizeRunAsync(factory, activeRunId.Value, completed.Value);

                runningIndex.Set(-1);
                runQueue.Set([]);
                runFinished.Set(true);
                refreshToken.Refresh();
                return;
            }

            refreshToken.Refresh();
            await Task.Yield();

            var result = await IvyAskService.AskAsync(questions[idx], BaseUrl);
            completed.Set([.. completed.Value, result]);

            if (persistToDb.Value && activeRunId.Value != Guid.Empty)
                _ = SaveResultAsync(factory, activeRunId.Value, result);

            refreshToken.Refresh();
            runningIndex.Set(idx + 1);
        }, [runningIndex.ToTrigger()]);

        var questionsQuery = UseQuery<List<TestQuestion>, string>(
            key: $"questions-{difficultyFilter.Value}",
            fetcher: async (_, ct) =>
            {
                var result = await LoadQuestionsAsync(factory, difficultyFilter.Value);
                refreshToken.Refresh();
                return result;
            });

        var isRunning = runningIndex.Value >= 0;
        var questions = isRunning && runQueue.Value.Count > 0 ? runQueue.Value : questionsQuery.Value ?? [];

        var done = completed.Value.Count;
        var success = completed.Value.Count(r => r.Status == "success");
        var noAnswer = completed.Value.Count(r => r.Status == "no_answer");
        var errors = completed.Value.Count(r => r.Status == "error");
        var avgMs = done > 0 ? (int)completed.Value.Average(r => r.ResponseTimeMs) : 0;
        var progressPct = questions.Count > 0 ? done * 100 / questions.Count : 0;

        var rows = questions.Select((q, i) =>
        {
            var r = completed.Value.FirstOrDefault(x => x.Question.Id == q.Id);
            Icons icon;
            string status, time;
            if (r != null)
            {
                icon = r.Status == "success" ? Icons.CircleCheck : Icons.CircleX;
                status = ToStatusLabel(r.Status);
                time = $"{r.ResponseTimeMs}ms";
            }
            else if (i == runningIndex.Value)
            {
                icon = Icons.Loader;
                status = "running";
                time = "";
            }
            else
            {
                icon = Icons.Clock;
                status = "pending";
                time = "";
            }
            return new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, icon, status, time);
        }).ToList();

        var controls = Layout.Horizontal().Height(Size.Fit()).Gap(2)
            | ivyVersion.ToTextInput().Placeholder("e.g. v2.4.0").Disabled(isRunning)
            | difficultyFilter.ToSelectInput(DifficultyOptions).Disabled(isRunning)
            | new Badge($"{questions.Count} questions")
            | new Button("Run All",
                onClick: async _ =>
                {
                    var snapshot = questionsQuery.Value ?? [];
                    if (snapshot.Count == 0) return;

                    var version = ivyVersion.Value.Trim();
                    if (string.IsNullOrEmpty(version))
                    {
                        client.Toast("Please enter an Ivy version before running.");
                        return;
                    }

                    var shouldPersist = !await RunExistsAsync(factory, version);
                    persistToDb.Set(shouldPersist);

                    if (shouldPersist)
                    {
                        var runId = await CreateRunAsync(factory, version, snapshot.Count);
                        activeRunId.Set(runId);
                    }
                    else
                    {
                        activeRunId.Set(Guid.Empty);
                    }

                    completed.Set([]);
                    runFinished.Set(false);
                    runQueue.Set(snapshot);
                    runningIndex.Set(0);
                })
                .Primary()
                .Icon(Icons.Play)
                .Disabled(isRunning || questionsQuery.Loading || questions.Count == 0);

        object statusBar;
        if (isRunning)
        {
            var currentQ = runningIndex.Value < questions.Count
                ? questions[runningIndex.Value]
                : null;
            var label = currentQ != null
                ? $"Running {done + 1}/{questions.Count}: {currentQ.Widget} — {currentQ.Question}"
                : $"Finishing…";

            statusBar = new Callout(
                Layout.Vertical().Gap(2)
                    | Text.Block(label)
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

        object kpiCards;
        if (done > 0)
        {
            var rate = done > 0 ? Math.Round(success * 100.0 / done, 1) : 0;
            kpiCards = Layout.Grid().Columns(4).Gap(3).Height(Size.Fit())
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3($"{rate}%")
                        | Text.Block($"{success} of {done} answered").Muted()
                ).Title("Answer Rate").Icon(Icons.CircleCheck)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3($"{noAnswer}")
                        | Text.Block("no answer").Muted()
                ).Title("No Answer").Icon(Icons.Ban)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3($"{errors}")
                        | Text.Block("failed").Muted()
                ).Title("Errors").Icon(Icons.CircleX)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3($"{avgMs} ms")
                        | Text.Block($"fastest {(done > 0 ? completed.Value.Min(r => r.ResponseTimeMs) : 0)} ms · slowest {(done > 0 ? completed.Value.Max(r => r.ResponseTimeMs) : 0)} ms").Muted()
                ).Title("Avg Response").Icon(Icons.Timer);
        }
        else
        {
            kpiCards = Text.Muted("");
        }

        var table = rows.AsQueryable()
            .ToDataTable()
            .RefreshToken(refreshToken)
            .Key("run-tests-table")
            .Height(Size.Full())
            .Hidden(r => r.Id)
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Difficulty, "Difficulty")
            .Header(r => r.Question, "Question")
            .Header(r => r.ResultIcon, "")
            .Header(r => r.Status, "Status")
            .Header(r => r.Time, "Time")
            .Width(r => r.ResultIcon, Size.Px(40))
            .Width(r => r.Widget, Size.Px(140))
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

        return Layout.Vertical().Gap(3).Height(Size.Full())
               | controls
               | statusBar
               | kpiCards
               | table;
    }

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

    private static async Task<Guid> CreateRunAsync(AppDbContextFactory factory, string ivyVersion, int totalQuestions)
    {
        await using var ctx = factory.CreateDbContext();
        var run = new TestRunEntity
        {
            IvyVersion = ivyVersion,
            TotalQuestions = totalQuestions,
            StartedAt = DateTime.UtcNow
        };
        ctx.TestRuns.Add(run);
        await ctx.SaveChangesAsync();
        return run.Id;
    }

    private static async Task SaveResultAsync(AppDbContextFactory factory, Guid testRunId, QuestionRun result)
    {
        try
        {
            if (!Guid.TryParse(result.Question.Id, out var questionId)) return;
            await using var ctx = factory.CreateDbContext();
            ctx.TestResults.Add(new TestResultEntity
            {
                TestRunId = testRunId,
                QuestionId = questionId,
                ResponseText = result.AnswerText,
                ResponseTimeMs = result.ResponseTimeMs,
                IsSuccess = result.Status == "success",
                HttpStatus = result.HttpStatus,
                ErrorMessage = result.Status == "error" ? result.AnswerText : null
            });
            await ctx.SaveChangesAsync();
        }
        catch
        {
            // best-effort
        }
    }

    private static async Task FinalizeRunAsync(AppDbContextFactory factory, Guid runId, List<QuestionRun> results)
    {
        try
        {
            await using var ctx = factory.CreateDbContext();
            var run = await ctx.TestRuns.FindAsync(runId);
            if (run == null) return;

            run.SuccessCount = results.Count(r => r.Status == "success");
            run.NoAnswerCount = results.Count(r => r.Status == "no_answer");
            run.ErrorCount = results.Count(r => r.Status == "error");
            run.CompletedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
        }
        catch
        {
            // best-effort
        }
    }

    private static string ToStatusLabel(string status) => status switch
    {
        "success" => "answered",
        "no_answer" => "no answer",
        "error" => "error",
        _ => status.Replace('_', ' ')
    };
}
