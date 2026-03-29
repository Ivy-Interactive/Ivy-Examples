namespace IvyAskStatistics.Apps;

[App(icon: Icons.Database, title: "Questions")]
public class QuestionsApp : ViewBase
{
    private class CellBuilder(Func<WidgetRow, object?, object?> build) : IBuilder<WidgetRow>
    {
        public object? Build(object? value, WidgetRow record) => build(record, value);
    }

    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();
        var configuration = UseService<IConfiguration>();
        var client = UseService<IClientProvider>();

        // ── State ─────────────────────────────────────────────────────────────
        var openAiKey = UseState(configuration["OpenAI:ApiKey"] ?? "");
        var generateRequest = UseState<IvyWidget?>(null);
        var isGenerating = UseState(false);
        var generatingStatus = UseState("");
        var refreshTick = UseState(0);

        // ── Hooks ─────────────────────────────────────────────────────────────
        // Async generate flow triggered by state change
        UseEffect(async () =>
        {
            var widget = generateRequest.Value;
            if (widget == null) return;

            isGenerating.Set(true);
            try
            {
                await QuestionGeneratorService.GenerateAndSaveAsync(
                    widget,
                    openAiKey.Value,
                    factory,
                    new Progress<string>(msg => generatingStatus.Set(msg)));

                refreshTick.Set(refreshTick.Value + 1);
                client.Toast($"Generated 30 questions for {widget.Name}");
            }
            catch (Exception ex)
            {
                client.Toast($"Error generating questions for {widget.Name}: {ex.Message}");
            }
            finally
            {
                isGenerating.Set(false);
                generatingStatus.Set("");
                generateRequest.Set(null);
            }
        }, [generateRequest.ToTrigger()]);

        // ── Queries ───────────────────────────────────────────────────────────
        var widgetsQuery = UseQuery<List<IvyWidget>, string>(
            key: "ivy-widgets",
            fetcher: async (_, ct) => await IvyAskService.GetWidgetsAsync());

        var countsQuery = UseQuery<Dictionary<string, (int easy, int medium, int hard)>, int>(
            key: refreshTick.Value,
            fetcher: async (_, ct) =>
            {
                await using var ctx = factory.CreateDbContext();
                var grouped = await ctx.Questions
                    .GroupBy(q => new { q.Widget, q.Difficulty })
                    .Select(g => new { g.Key.Widget, g.Key.Difficulty, Count = g.Count() })
                    .ToListAsync(ct);

                return grouped
                    .GroupBy(x => x.Widget)
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            easy: g.FirstOrDefault(x => x.Difficulty == "easy")?.Count ?? 0,
                            medium: g.FirstOrDefault(x => x.Difficulty == "medium")?.Count ?? 0,
                            hard: g.FirstOrDefault(x => x.Difficulty == "hard")?.Count ?? 0
                        ));
            });

        // ── Build rows ────────────────────────────────────────────────────────
        var widgets = widgetsQuery.Value ?? [];
        var counts = countsQuery.Value ?? new Dictionary<string, (int, int, int)>();

        var rows = widgets.Select(w =>
        {
            var (easy, medium, hard) = counts.GetValueOrDefault(w.Name);
            return new WidgetRow(w.Name, w.Category, easy, medium, hard);
        }).ToList(); // Actions defaults to ""

        // ── OpenAI key + seed bar ─────────────────────────────────────────────
        var hasKey = !string.IsNullOrWhiteSpace(openAiKey.Value);
        var totalQuestions = rows.Sum(r => r.Easy + r.Medium + r.Hard);

        var headerCard = new Card(
            Layout.Vertical().Gap(3)
            | (Layout.Horizontal().Gap(3)
               | (Layout.Vertical().Gap(1)
                  | Text.Block("OpenAI API Key").Bold().Small()
                  | openAiKey.ToPasswordInput().Placeholder("sk-…").Width(Size.Units(80)))
               | new Spacer()
               | Text.Block($"{totalQuestions} total questions in DB").Muted().Small())
            | (isGenerating.Value
                ? (object)(Layout.Horizontal().Gap(2)
                   | new Icon(Icons.Loader).Small()
                   | Text.Muted(generatingStatus.Value))
                : null)
        );

        // ── Widgets table ─────────────────────────────────────────────────────
        if (widgetsQuery.Loading)
        {
            return Layout.Vertical()
                   | headerCard
                   | new Progress(-1).Goal("Loading widgets…");
        }

        var table = new TableBuilder<WidgetRow>(rows)
            .Builder(x => x.Easy, _ => new CellBuilder((row, _) =>
                row.Easy > 0
                    ? (object)new Badge(row.Easy.ToString()).Variant(BadgeVariant.Success)
                    : Text.Muted("–")))
            .Builder(x => x.Medium, _ => new CellBuilder((row, _) =>
                row.Medium > 0
                    ? (object)new Badge(row.Medium.ToString()).Variant(BadgeVariant.Info)
                    : Text.Muted("–")))
            .Builder(x => x.Hard, _ => new CellBuilder((row, _) =>
                row.Hard > 0
                    ? (object)new Badge(row.Hard.ToString()).Variant(BadgeVariant.Destructive)
                    : Text.Muted("–")))
            .Builder(x => x.Actions, _ => new CellBuilder((row, _) =>
                new Button("Generate", onClick: _ =>
                {
                    if (!hasKey)
                    {
                        client.Toast("Enter your OpenAI API key first");
                        return;
                    }
                    var widget = widgets.FirstOrDefault(w => w.Name == row.Widget);
                    if (widget != null) generateRequest.Set(widget);
                })
                .Small()
                .Variant(ButtonVariant.Outline)
                .Disabled(isGenerating.Value)
                .Icon(Icons.Sparkles)))
            .Build();

        return Layout.Vertical()
               | headerCard
               | table;
    }

}
