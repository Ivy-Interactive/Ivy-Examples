namespace IvyAskStatistics.Apps;

[App(icon: Icons.ChartBar, title: "IVY Ask Statistics")]
public class RunApp : ViewBase
{
    private static readonly string[] DifficultyOptions = ["all", "easy", "medium", "hard"];
    private const string BaseUrl = "https://mcp.ivy.app";

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();
        var client = UseService<IClientProvider>();

        // ── State ─────────────────────────────────────────────────────────────
        var difficultyFilter = UseState("all");
        var runningIndex = UseState(-1);
        var completed = UseState<List<QuestionRun>>([]);

        // ── Hooks (must be at top, before derived values) ─────────────────────
        UseEffect(() =>
        {
            completed.Set([]);
            runningIndex.Set(-1);
        }, [difficultyFilter.ToTrigger()]);

        UseEffect(async () =>
        {
            var idx = runningIndex.Value;
            if (idx < 0) return;

            var questions = await LoadQuestionsAsync(factory, difficultyFilter.Value);

            if (idx >= questions.Count)
            {
                runningIndex.Set(-1);
                var s = completed.Value.Count(r => r.Status == "success");
                client.Toast($"Done! {s}/{questions.Count} answered");
                return;
            }

            var result = await IvyAskService.AskAsync(questions[idx], BaseUrl);
            completed.Set([.. completed.Value, result]);
            runningIndex.Set(idx + 1);
        }, [runningIndex.ToTrigger()]);

        // ── Queries ───────────────────────────────────────────────────────────
        var questionsQuery = UseQuery<List<TestQuestion>, string>(
            key: $"questions-{difficultyFilter.Value}",
            fetcher: async (_, ct) => await LoadQuestionsAsync(factory, difficultyFilter.Value));

        var questions = questionsQuery.Value ?? [];
        var isRunning = runningIndex.Value >= 0;

        // ── Derived values ────────────────────────────────────────────────────
        var done = completed.Value.Count;
        var success = completed.Value.Count(r => r.Status == "success");
        var noAnswer = completed.Value.Count(r => r.Status == "no_answer");
        var errors = completed.Value.Count(r => r.Status == "error");
        var avgMs = done > 0 ? (int)completed.Value.Average(r => r.ResponseTimeMs) : 0;
        var progressPct = questions.Count > 0 ? done * 100 / questions.Count : 0;

        // ── Build display rows ────────────────────────────────────────────────
        var rows = questions.Select((q, i) =>
        {
            var r = completed.Value.FirstOrDefault(x => x.Question.Id == q.Id);
            var status = r?.Status ?? (i == runningIndex.Value ? "running" : "pending");
            return new QuestionRow(q.Id, q.Widget, q.Difficulty, q.Question, status, r?.ResponseTimeMs);
        }).ToList();

        // ── Controls bar ──────────────────────────────────────────────────────
        var controls = new Card(
            Layout.Horizontal()
            | difficultyFilter.ToSelectInput(DifficultyOptions).Disabled(isRunning)
            | Text.Muted($"{questions.Count} questions")
            | new Spacer()
            | new Button(isRunning ? "Running…" : "Run All",
                onClick: _ =>
                {
                    completed.Set([]);
                    runningIndex.Set(0);
                })
                .Primary()
                .Icon(isRunning ? Icons.Loader : Icons.Play)
                .Disabled(isRunning || questionsQuery.Loading || questions.Count == 0)
        );

        // ── Progress bar ──────────────────────────────────────────────────────
        object? progressSection = isRunning || done > 0
            ? new Progress(progressPct).Goal($"{done} / {questions.Count} questions")
            : null;

        // ── Summary card (only after run completes) ───────────────────────────
        object? summarySection = done > 0 && !isRunning
            ? BuildSummary(success, noAnswer, errors, done, avgMs)
            : null;

        // ── Questions DataTable ───────────────────────────────────────────────
        var table = rows.AsQueryable()
            .ToDataTable()
            .Hidden(r => r.Id)
            .Hidden(r => r.ResponseTimeMs)
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Difficulty, "Difficulty")
            .Header(r => r.Question, "Question")
            .Header(r => r.Status, "Status")
            .Width(r => r.Widget, Size.Px(120))
            .Width(r => r.Difficulty, Size.Px(100))
            .Width(r => r.Status, Size.Px(120))
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
                config.ShowIndexColumn = true;
            });

        return Layout.Vertical()
               | controls
               | progressSection
               | summarySection
               | table;
    }

    private static async Task<List<TestQuestion>> LoadQuestionsAsync(
        AppDbContextFactory factory,
        string difficulty)
    {
        await using var ctx = factory.CreateDbContext();
        var query = ctx.Questions.AsQueryable();
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

    private static object BuildSummary(int success, int noAnswer, int errors, int total, int avgMs)
    {
        var rate = total > 0 ? success * 100 / total : 0;

        return new Card(
            Layout.Horizontal()
            | new Details([new Detail("Success Rate", $"{rate}%  ({success}/{total})", false)])
            | new Details([new Detail("No Answer", noAnswer.ToString(), false)])
            | new Details([new Detail("Errors", errors.ToString(), false)])
            | new Details([new Detail("Avg Time", $"{avgMs} ms", false)])
        );
    }
}
