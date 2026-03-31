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

        var difficultyFilter = UseState("all");
        var runningIndex = UseState(-1);
        var completed = UseState<List<QuestionRun>>([]);
        var refreshToken = UseRefreshToken();

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
                refreshToken.Refresh();
                var s = completed.Value.Count(r => r.Status == "success");
                client.Toast($"Done! {s}/{questions.Count} answered");
                return;
            }

            var result = await IvyAskService.AskAsync(questions[idx], BaseUrl);
            completed.Set([.. completed.Value, result]);
            refreshToken.Refresh();
            runningIndex.Set(idx + 1);
        }, [runningIndex.ToTrigger()]);

        var questionsQuery = UseQuery<List<TestQuestion>, string>(
            key: $"questions-{difficultyFilter.Value}",
            fetcher: async (_, ct) => await LoadQuestionsAsync(factory, difficultyFilter.Value));

        var questions = questionsQuery.Value ?? [];
        var isRunning = runningIndex.Value >= 0;

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
                status = "in progress";
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

        var controls = Layout.Horizontal().Height(Size.Fit())
            | difficultyFilter.ToSelectInput(DifficultyOptions).Disabled(isRunning)
            | Text.Muted($"{questions.Count} questions")
            | (isRunning ? new Progress(progressPct).Goal($"{done}/{questions.Count}") : null)
            | new Button(isRunning ? "Running…" : "Run All",
                onClick: _ =>
                {
                    completed.Set([]);
                    runningIndex.Set(0);
                })
                .Primary()
                .Icon(isRunning ? Icons.Loader : Icons.Play)
                .Disabled(isRunning || questionsQuery.Loading || questions.Count == 0);

        var table = rows.AsQueryable()
            .ToDataTable()
            .RefreshToken(refreshToken)
            .Key("run-tests-table")
            .Height(Size.Full())
            .Hidden(r => r.Id)
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Difficulty, "Difficulty")
            .Header(r => r.Question, "Question")
            .Header(r => r.ResultIcon, "Icon")
            .Header(r => r.Status, "Status")
            .Header(r => r.Time, "Time")
            .Width(r => r.ResultIcon, Size.Px(50))
            .Width(r => r.Widget, Size.Px(120))
            .Width(r => r.Difficulty, Size.Px(80))
            .Width(r => r.Status, Size.Px(100))
            .Width(r => r.Time, Size.Px(80))
            .Width(r => r.Question, Size.Px(400))
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
                config.ShowIndexColumn = true;
            });

        return Layout.Vertical().Height(Size.Full())
               | controls
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

    private static string ToStatusLabel(string status) => status switch
    {
        "success" => "answered",
        "no_answer" => "no answer",
        "error" => "error",
        _ => status.Replace('_', ' ')
    };
}
