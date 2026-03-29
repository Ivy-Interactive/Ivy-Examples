namespace IvyAskStatistics.Apps;

[App(icon: Icons.Database, title: "Questions")]
public class QuestionsApp : ViewBase
{
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
        var hasKey = !string.IsNullOrWhiteSpace(openAiKey.Value);
        var totalQuestions = counts.Values.Sum(c => c.easy + c.medium + c.hard);

        var rows = widgets.Select(w =>
        {
            var (easy, medium, hard) = counts.GetValueOrDefault(w.Name);
            return new WidgetRow(w.Name, w.Category, easy, medium, hard);
        }).ToList();

        // ── Header card ───────────────────────────────────────────────────────
        var headerCard = new Card(
            Layout.Vertical().Gap(3)
            | (Layout.Horizontal().Gap(3)
               | (Layout.Vertical().Gap(1)
                  | Text.Block("OpenAI API Key").Bold().Small()
                  | openAiKey.ToPasswordInput().Placeholder("sk-…").Width(Size.Units(80)))
               | new Spacer()
               | Text.Block($"{totalQuestions} total questions in DB").Muted().Small())
            | (widgetsQuery.Loading
                ? (object)(Layout.Horizontal().Gap(2)
                   | new Icon(Icons.Loader).Small()
                   | Text.Muted("Loading widgets…"))
                : isGenerating.Value
                    ? (Layout.Horizontal().Gap(2)
                       | new Icon(Icons.Loader).Small()
                       | Text.Muted(generatingStatus.Value))
                    : null)
        );

        // ── Widgets DataTable ─────────────────────────────────────────────────
        var table = rows.AsQueryable()
            .ToDataTable(r => r.Widget)
            .Header(r => r.Widget, "Widget")
            .Header(r => r.Category, "Category")
            .Header(r => r.Easy, "Easy")
            .Header(r => r.Medium, "Medium")
            .Header(r => r.Hard, "Hard")
            .Width(r => r.Category, Size.Px(120))
            .Width(r => r.Easy, Size.Px(70))
            .Width(r => r.Medium, Size.Px(80))
            .Width(r => r.Hard, Size.Px(70))
            .RowActions(
                MenuItem.Default(Icons.Sparkles, "generate").Label("Generate").Tag("generate"))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args?.Tag?.ToString() != "generate") return ValueTask.CompletedTask;
                if (!hasKey)
                {
                    client.Toast("Enter your OpenAI API key first");
                    return ValueTask.CompletedTask;
                }
                var widget = widgets.FirstOrDefault(w => w.Name == args.Id?.ToString());
                if (widget != null) generateRequest.Set(widget);
                return ValueTask.CompletedTask;
            })
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
                config.ShowIndexColumn = false;
            });

        return Layout.Vertical()
               | headerCard
               | table;
    }
}
