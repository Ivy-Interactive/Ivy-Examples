using System.Collections.Immutable;

namespace IvyAskStatistics.Apps;

internal sealed record WidgetTableData(List<WidgetRow> Rows, List<IvyWidget> Catalog, int QueryKey);

internal sealed record GenProgress(string CurrentWidget, int Done, int Total, List<string> Failed, bool Active);

[App(icon: Icons.Database, title: "Questions")]
public class QuestionsApp : ViewBase
{
    private const int TableQueryKey = 0;

    public override object? Build()
    {
        var factory       = UseService<AppDbContextFactory>();
        var configuration = UseService<IConfiguration>();
        var client        = UseService<IClientProvider>();

        var generatingWidgets = UseState(ImmutableHashSet<string>.Empty);
        var deleteRequest     = UseState<string?>(null);
        var viewDialogOpen    = UseState(false);
        var viewDialogWidget  = UseState("");
        var editSheetOpen     = UseState(false);
        var editQuestionId    = UseState(Guid.Empty);
        var refreshToken      = UseRefreshToken();
        var genProgress       = UseState<GenProgress?>(null);
        var (alertView, showAlert) = UseAlert();

        var tableQuery = UseQuery<WidgetTableData, int>(
            key: TableQueryKey,
            fetcher: async (qk, ct) =>
            {
                var result = await LoadWidgetTableDataAsync(factory, qk, ct);
                refreshToken.Refresh();
                return result;
            },
            options: new QueryOptions { KeepPrevious = true, RefreshInterval = TimeSpan.FromSeconds(10), RevalidateOnMount = true },
            tags: ["widget-summary"]);

        UseEffect(async () =>
        {
            var widgetName = deleteRequest.Value;
            if (string.IsNullOrEmpty(widgetName)) return;

            try
            {
                await using var ctx = factory.CreateDbContext();
                var list = await ctx.Questions.Where(q => q.Widget == widgetName).ToListAsync();
                if (list.Count == 0) return;
                ctx.Questions.RemoveRange(list);
                await ctx.SaveChangesAsync();

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                refreshToken.Refresh();
            }
            catch
            {
                // best-effort
            }
            finally
            {
                deleteRequest.Set(null);
            }
        }, [deleteRequest.ToTrigger()]);

        async Task GenerateOneAsync(IvyWidget widget)
        {
            var apiKey  = configuration["OpenAI:ApiKey"]  ?? throw new InvalidOperationException("OpenAI:ApiKey secret is not set.");
            var baseUrl = configuration["OpenAI:BaseUrl"] ?? throw new InvalidOperationException("OpenAI:BaseUrl secret is not set.");
            await QuestionGeneratorService.GenerateAndSaveAsync(widget, factory, apiKey, baseUrl);
        }

        async Task GenerateWidgetAsync(IvyWidget widget)
        {
            try
            {
                genProgress.Set(new GenProgress(widget.Name, 0, 1, [], true));
                await GenerateOneAsync(widget);

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                genProgress.Set(new GenProgress(widget.Name, 1, 1, [], false));
            }
            catch
            {
                genProgress.Set(new GenProgress(widget.Name, 0, 1, [widget.Name], false));
            }
            finally
            {
                generatingWidgets.Set(s => s.Remove(widget.Name));
                refreshToken.Refresh();
            }
        }

        async Task GenerateBatchAsync(List<IvyWidget> widgets)
        {
            const int maxRetries = 2;
            var failed = new List<string>();
            var done = 0;

            foreach (var widget in widgets)
            {
                genProgress.Set(new GenProgress(widget.Name, done, widgets.Count, failed, true));
                refreshToken.Refresh();

                var success = false;
                for (var attempt = 1; attempt <= maxRetries && !success; attempt++)
                {
                    try
                    {
                        await GenerateOneAsync(widget);
                        success = true;
                    }
                    catch
                    {
                        if (attempt < maxRetries)
                            await Task.Delay(2000);
                    }
                }

                if (success)
                    done++;
                else
                    failed.Add(widget.Name);

                generatingWidgets.Set(s => s.Remove(widget.Name));

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                refreshToken.Refresh();
            }

            generatingWidgets.Set(_ => ImmutableHashSet<string>.Empty);
            genProgress.Set(new GenProgress("", done, widgets.Count, failed, false));
            refreshToken.Refresh();
        }

        void MarkGenerating(IEnumerable<string> widgetNames)
        {
            var names = widgetNames.Where(n => !string.IsNullOrEmpty(n)).ToHashSet();
            if (names.Count == 0) return;
            generatingWidgets.Set(s => s.Union(names).ToImmutableHashSet());
            refreshToken.Refresh();
        }

        void OnGenerateAll()
        {
            var allWidgets = tableQuery.Value?.Catalog ?? [];
            if (allWidgets.Count == 0) return;

            var allRows = tableQuery.Value?.Rows ?? [];
            var notGenerated = allWidgets
                .Where(w => !allRows.Any(r => r.Widget == w.Name && r.Easy + r.Medium + r.Hard > 0))
                .ToList();

            if (notGenerated.Count == 0) return;

            showAlert(
                $"Generate questions for {notGenerated.Count} widget(s) that don't have questions yet?\n\nOpenAI will be called 3 times per widget (easy / medium / hard).",
                result =>
                {
                    if (!result.IsOk()) return;
                    MarkGenerating(notGenerated.Select(w => w.Name));
                    genProgress.Set(new GenProgress(notGenerated[0].Name, 0, notGenerated.Count, [], true));
                    Task.Run(() => GenerateBatchAsync(notGenerated));
                },
                "Generate All Questions",
                AlertButtonSet.OkCancel);
        }

        UseEffect(() =>
        {
            if (tableQuery.Value == null) return;
            if (!GenerateAllBridge.Consume()) return;
            OnGenerateAll();
        }, EffectTrigger.OnBuild());

        var generating = generatingWidgets.Value;
        var baseRows   = tableQuery.Value?.Rows ?? [];
        var catalog    = tableQuery.Value?.Catalog ?? [];
        var isDeleting = !string.IsNullOrEmpty(deleteRequest.Value);
        var firstLoad  = tableQuery.Loading && tableQuery.Value == null;
        var progress   = genProgress.Value;
        var isGenerating = generating.Count > 0;

        static string IdleStatus(WidgetRow r)
        {
            var n = r.Easy + r.Medium + r.Hard;
            return n == 0 ? "○ Not generated" : "✓ Generated";
        }

        var rows = baseRows.Select(r =>
            generating.Contains(r.Widget)
                ? r with { Status = "Generating…" }
                : r with { Status = IdleStatus(r) }
        ).ToList();

        if (firstLoad)
            return Layout.Center()
                   | new Icon(Icons.Loader)
                   | Text.Muted("Loading…");

        var notGeneratedCount = baseRows.Count(r => r.Easy + r.Medium + r.Hard == 0);

        object progressBar;
        if (progress is { Active: true })
        {
            var pct = progress.Total > 0 ? progress.Done * 100 / progress.Total : 0;
            progressBar = new Callout(
                Layout.Vertical().Gap(2)
                    | Text.Block($"Generating {progress.Done + 1}/{progress.Total}: {progress.CurrentWidget}…")
                    | new Progress(pct).Goal($"{progress.Done}/{progress.Total}"),
                variant: CalloutVariant.Info);
        }
        else if (progress is { Active: false, Total: > 0 })
        {
            var failCount = progress.Failed.Count;
            progressBar = new Callout(
                Layout.Horizontal().Gap(2)
                    | Text.Block(failCount == 0
                        ? $"Done! Generated questions for {progress.Done}/{progress.Total} widget(s)."
                        : $"Completed: {progress.Done}/{progress.Total} succeeded. Failed: {string.Join(", ", progress.Failed)}")
                    | new Button("Dismiss", onClick: _ => genProgress.Set(null)).Small(),
                variant: failCount == 0 ? CalloutVariant.Success : CalloutVariant.Warning);
        }
        else
        {
            progressBar = Text.Muted("");
        }

        var table = rows.AsQueryable()
            .ToDataTable(r => r.Widget)
            .RefreshToken(refreshToken)
            .Key("questions-widgets")
            .Height(Size.Full())
            .Header(r => r.Widget,      "Widget")
            .Header(r => r.Category,    "Category")
            .Header(r => r.Easy,        "Easy")
            .Header(r => r.Medium,      "Medium")
            .Header(r => r.Hard,        "Hard")
            .Header(r => r.LastUpdated, "Last Generated")
            .Header(r => r.Status,      "Status")
            .Width(r => r.Widget,       Size.Px(160))
            .Width(r => r.Category,     Size.Px(120))
            .Width(r => r.Easy,         Size.Px(60))
            .Width(r => r.Medium,       Size.Px(70))
            .Width(r => r.Hard,         Size.Px(60))
            .Width(r => r.LastUpdated,  Size.Px(170))
            .Width(r => r.Status,       Size.Px(280))
            .RowActions(
                MenuItem.Default(Icons.List, "questions").Label("View questions").Tag("questions"),
                MenuItem.Default(Icons.Sparkles, "generate").Label("Generate questions").Tag("generate"),
                MenuItem.Default(Icons.Trash2, "delete").Label("Delete questions").Tag("delete"))
            .OnRowAction(e =>
            {
                var args = e.Value;
                var tag  = args?.Tag?.ToString();
                if (string.IsNullOrEmpty(tag)) return ValueTask.CompletedTask;

                if (tag == "questions")
                {
                    var viewName = args.Id?.ToString() ?? "";
                    if (string.IsNullOrEmpty(viewName)) return ValueTask.CompletedTask;
                    viewDialogWidget.Set(viewName);
                    viewDialogOpen.Set(true);
                    return ValueTask.CompletedTask;
                }

                if (tag == "generate")
                {
                    if (isDeleting || isGenerating) return ValueTask.CompletedTask;

                    var genName = args.Id?.ToString() ?? "";
                    if (generating.Contains(genName)) return ValueTask.CompletedTask;

                    var widget = catalog.FirstOrDefault(w => w.Name == genName)
                        ?? new IvyWidget(genName, rows.FirstOrDefault(r => r.Widget == genName)?.Category ?? "", "");

                    showAlert(
                        $"Generate 30 questions for the \"{widget.Name}\" widget?\n\nOpenAI will be called three times (easy / medium / hard). Any previously generated questions for this widget will be replaced.",
                        result =>
                        {
                            if (!result.IsOk()) return;
                            MarkGenerating([widget.Name]);
                            _ = GenerateWidgetAsync(widget);
                        },
                        "Generate questions",
                        AlertButtonSet.OkCancel);
                    return ValueTask.CompletedTask;
                }

                if (tag == "delete")
                {
                    if (isDeleting || isGenerating) return ValueTask.CompletedTask;

                    var delName = args.Id?.ToString() ?? "";
                    if (generating.Contains(delName)) return ValueTask.CompletedTask;

                    var row = rows.FirstOrDefault(r => r.Widget == delName);
                    var n   = row == null ? 0 : row.Easy + row.Medium + row.Hard;
                    if (n == 0) return ValueTask.CompletedTask;

                    showAlert(
                        $"Delete all {n} question(s) for the \"{delName}\" widget?\n\nThis cannot be undone.",
                        result =>
                        {
                            if (!result.IsOk()) return;
                            deleteRequest.Set(delName);
                        },
                        "Delete questions",
                        AlertButtonSet.OkCancel);
                    return ValueTask.CompletedTask;
                }

                return ValueTask.CompletedTask;
            })
            .Config(config =>
            {
                config.AllowSorting    = true;
                config.AllowFiltering  = true;
                config.ShowSearch      = true;
                config.ShowIndexColumn = false;
            });

        object? questionsDialog = viewDialogOpen.Value && !string.IsNullOrEmpty(viewDialogWidget.Value)
            ? new WidgetQuestionsDialog(viewDialogOpen, viewDialogWidget.Value, editSheetOpen, editQuestionId)
            : null;

        object? editSheet = editSheetOpen.Value && editQuestionId.Value != Guid.Empty
            ? new QuestionEditSheet(editSheetOpen, editQuestionId.Value)
            : null;

        return Layout.Vertical().Height(Size.Full())
               | alertView
               | progressBar
               | table
               | questionsDialog
               | editSheet;
    }

    private static async Task<WidgetTableData> LoadWidgetTableDataAsync(
        AppDbContextFactory factory,
        int queryKey,
        CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var grouped = await ctx.Questions
            .AsNoTracking()
            .GroupBy(q => new { q.Widget, q.Category, q.Difficulty })
            .Select(g => new
            {
                g.Key.Widget,
                g.Key.Category,
                g.Key.Difficulty,
                Count   = g.Count(),
                MaxDate = g.Max(x => x.CreatedAt)
            })
            .ToListAsync(ct);

        var countsByWidget = grouped
            .GroupBy(x => x.Widget)
            .ToDictionary(
                g => g.Key,
                g => (
                    category:  g.Select(x => x.Category).FirstOrDefault() ?? "",
                    easy:      g.FirstOrDefault(x => x.Difficulty == "easy")?.Count   ?? 0,
                    medium:    g.FirstOrDefault(x => x.Difficulty == "medium")?.Count ?? 0,
                    hard:      g.FirstOrDefault(x => x.Difficulty == "hard")?.Count   ?? 0,
                    updatedAt: g.Max(x => x.MaxDate)
                ));

        List<IvyWidget> catalog = [];
        try { catalog = await IvyAskService.GetWidgetsAsync(); }
        catch { }

        var byName = catalog.ToDictionary(w => w.Name);
        foreach (var (widget, info) in countsByWidget)
            if (!byName.ContainsKey(widget))
                byName[widget] = new IvyWidget(widget, info.category, "");

        var rows = byName.Values
            .OrderBy(w => string.IsNullOrEmpty(w.Category) ? "zzz" : w.Category)
            .ThenBy(w => w.Name)
            .Select(w =>
            {
                var c        = countsByWidget.GetValueOrDefault(w.Name);
                var category = string.IsNullOrEmpty(w.Category) ? "Unclassified" : w.Category;
                var updated  = c.updatedAt == default
                    ? "—"
                    : c.updatedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
                return new WidgetRow(w.Name, category, c.easy, c.medium, c.hard, updated, "");
            })
            .ToList();

        return new WidgetTableData(rows, catalog, queryKey);
    }
}
