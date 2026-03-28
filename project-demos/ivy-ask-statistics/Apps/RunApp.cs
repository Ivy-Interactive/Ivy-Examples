namespace IvyAskStatistics.Apps;

[App(icon: Icons.ChartBar, title: "IVY Ask Statistics")]
public class RunApp : ViewBase
{
    private static readonly string[] DifficultyOptions = ["all", "easy", "medium", "hard"];
    private const string BaseUrl = "https://mcp.ivy.app";

    // Implements IBuilder<T> so TableBuilder can render custom cells
    private class CellBuilder(Func<QuestionRow, object?, object?> build) : IBuilder<QuestionRow>
    {
        public object? Build(object? value, QuestionRow record) => build(record, value);
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // ── State ─────────────────────────────────────────────────────────────
        var difficultyFilter = UseState("all");

        // runningIndex drives the step-by-step runner:
        //   -1       = idle
        //   0..n-1   = currently processing question at that index
        //   ≥ count  = finished (effect resets to -1)
        var runningIndex = UseState(-1);
        var completed = UseState<List<QuestionRun>>([]);

        // ── Hooks (must be at top, before derived values) ─────────────────────
        // Reset when difficulty filter changes
        UseEffect(() =>
        {
            completed.Set([]);
            runningIndex.Set(-1);
        }, [difficultyFilter.ToTrigger()]);

        // Step runner: each index change processes one question then increments
        UseEffect(async () =>
        {
            var idx = runningIndex.Value;
            if (idx < 0) return;

            var questions = GetFiltered(difficultyFilter.Value);

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

        // ── Derived values ────────────────────────────────────────────────────
        var questions = GetFiltered(difficultyFilter.Value);
        var isRunning = runningIndex.Value >= 0;

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
            | new Spacer()
            | new Button(isRunning ? "Running…" : "Run All",
                onClick: _ =>
                {
                    completed.Set([]);
                    runningIndex.Set(0);
                })
                .Primary()
                .Icon(isRunning ? Icons.Loader : Icons.Play)
                .Disabled(isRunning)
        );

        // ── Progress bar ──────────────────────────────────────────────────────
        object? progressSection = isRunning || done > 0
            ? new Progress(progressPct).Goal($"{done} / {questions.Count} questions")
            : null;

        // ── Summary card (only after run completes) ───────────────────────────
        object? summarySection = done > 0 && !isRunning
            ? BuildSummary(success, noAnswer, errors, done, avgMs)
            : null;

        // ── Questions table ───────────────────────────────────────────────────
        var table = new TableBuilder<QuestionRow>(rows)
            .Builder(x => x.Difficulty, _ => new CellBuilder((row, _) =>
                new Badge(row.Difficulty).Variant(row.Difficulty switch
                {
                    "easy" => BadgeVariant.Success,
                    "medium" => BadgeVariant.Info,
                    "hard" => BadgeVariant.Destructive,
                    _ => BadgeVariant.Secondary
                })))
            .Builder(x => x.Status, _ => new CellBuilder((row, _) =>
                row.Status switch
                {
                    "success" => (object)(Layout.Horizontal()
                        | new Badge("answered").Variant(BadgeVariant.Success)
                        | Text.Muted($"{row.ResponseTimeMs}ms")),
                    "no_answer" => new Badge("no answer").Variant(BadgeVariant.Destructive),
                    "error" => new Badge("error").Variant(BadgeVariant.Warning),
                    "running" => new Badge("running…").Variant(BadgeVariant.Info),
                    _ => new Badge("pending").Variant(BadgeVariant.Secondary)
                }))
            .Remove(x => x.Id)
            .Remove(x => x.ResponseTimeMs)
            .Build();

        return Layout.Vertical()
               | controls
               | progressSection
               | summarySection
               | table;
    }

    private static object BuildSummary(int success, int noAnswer, int errors, int total, int avgMs)
    {
        var rate = total > 0 ? success * 100 / total : 0;

        return new Card(
            Layout.Horizontal()
            | new Details([
                new Detail("Success Rate", $"{rate}%  ({success}/{total})", false),
            ])
            | new Details([
                new Detail("No Answer", noAnswer.ToString(), false),
            ])
            | new Details([
                new Detail("Errors", errors.ToString(), false),
            ])
            | new Details([
                new Detail("Avg Time", $"{avgMs} ms", false),
            ])
        );
    }

    private static List<TestQuestion> GetFiltered(string difficulty) =>
        difficulty == "all"
            ? Questions.All
            : Questions.All.Where(q => q.Difficulty == difficulty).ToList();
}
