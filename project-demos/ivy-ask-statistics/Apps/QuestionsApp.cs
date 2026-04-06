using System.Collections.Immutable;
using System.Threading;

namespace IvyAskStatistics.Apps;

internal sealed record WidgetTableData(List<WidgetRow> Rows, List<IvyWidget> Catalog, int QueryKey);

internal sealed record GenProgress(
    string CurrentWidget,
    int Done,
    int Total,
    List<string> Failed,
    bool Active,
    int MaxParallel = 1);

[App(icon: Icons.Database, title: "Questions")]
public class QuestionsApp : ViewBase
{
    private const int TableQueryKey = 0;

    /// <summary>Batch “Generate all” runs this many widgets concurrently (no config).</summary>
    private const int WidgetGenerationParallelism = 4;

    public override object? Build()
    {
        var factory       = UseService<AppDbContextFactory>();
        var configuration = UseService<IConfiguration>();
        var client        = UseService<IClientProvider>();
        var queryService  = UseService<IQueryService>();

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

        // Register flush on every build (fresh showAlert closure). Call Flush after bind so a pending
        // request that arrived before this view existed still opens the dialog. Request() also invokes
        // the handler so repeat footer clicks work when Navigate does not rebuild (same tab).
        UseEffect(() =>
        {
            void FlushFooterGenerateAll()
            {
                if (!GenerateAllBridge.Consume()) return;
                ShowFooterGenerateAllDialog();
            }

            GenerateAllBridge.SetFlushHandler(FlushFooterGenerateAll);
            FlushFooterGenerateAll();
        }, EffectTrigger.OnBuild());

        UseEffect(() => new GenerateAllFlushRegistration(), EffectTrigger.OnMount());

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
                queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
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
            var apiKey  = configuration[QuestionGeneratorService.ApiKeyConfigKey]!.Trim();
            var baseUrl = configuration[QuestionGeneratorService.BaseUrlConfigKey]!.Trim();
            await QuestionGeneratorService.GenerateAndSaveAsync(widget, factory, apiKey, baseUrl, configuration);
        }

        async Task GenerateWidgetAsync(IvyWidget widget)
        {
            try
            {
                genProgress.Set(new GenProgress(widget.Name, 0, 1, [], true, 1));
                await GenerateOneAsync(widget);

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
                genProgress.Set(new GenProgress(widget.Name, 1, 1, [], false, 1));
            }
            catch (Exception ex)
            {
                client.Toast(ex.Message);
                genProgress.Set(new GenProgress(widget.Name, 0, 1, [widget.Name], false, 1));
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
            var maxParallel = WidgetGenerationParallelism;
            var completed = 0;
            var failedLock = new object();
            var failed = new List<string>();

            using var sem = new SemaphoreSlim(maxParallel);
            using var uiGate = new SemaphoreSlim(1, 1);
            using var tickerCts = new CancellationTokenSource();
            using var ticker = new PeriodicTimer(TimeSpan.FromMilliseconds(800));

            async Task PushUiFromStateAsync()
            {
                await uiGate.WaitAsync();
                try
                {
                    var d = Volatile.Read(ref completed);
                    List<string> failedCopy;
                    lock (failedLock)
                        failedCopy = [..failed];
                    genProgress.Set(new GenProgress("", d, widgets.Count, failedCopy, true, maxParallel));
                    var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                    tableQuery.Mutator.Mutate(fresh, revalidate: false);
                    refreshToken.Refresh();
                }
                finally
                {
                    uiGate.Release();
                }
            }

            var uiTickerTask = Task.Run(async () =>
            {
                try
                {
                    while (await ticker.WaitForNextTickAsync(tickerCts.Token))
                        await PushUiFromStateAsync();
                }
                catch (OperationCanceledException)
                {
                    // expected when batch finishes
                }
            });

            try
            {
                await PushUiFromStateAsync();

                var workerTasks = widgets.Select(async widget =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        var success = false;
                        Exception? lastEx = null;
                        for (var attempt = 1; attempt <= maxRetries && !success; attempt++)
                        {
                            try
                            {
                                await GenerateOneAsync(widget);
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                lastEx = ex;
                                if (attempt < maxRetries)
                                    await Task.Delay(2000);
                            }
                        }

                        if (success)
                        {
                            Interlocked.Increment(ref completed);
                            queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
                        }
                        else
                        {
                            lock (failedLock)
                                failed.Add(widget.Name);
                            if (lastEx != null)
                                client.Toast($"\"{widget.Name}\": {lastEx.Message}");
                        }

                        await uiGate.WaitAsync();
                        try
                        {
                            generatingWidgets.Set(s => s.Remove(widget.Name));
                            refreshToken.Refresh();
                        }
                        finally
                        {
                            uiGate.Release();
                        }
                    }
                    finally
                    {
                        sem.Release();
                    }
                }).ToArray();

                await Task.WhenAll(workerTasks);
            }
            finally
            {
                tickerCts.Cancel();
                try
                {
                    await uiTickerTask;
                }
                catch
                {
                    // ignore cancellation teardown
                }

                ticker.Dispose();
            }

            await uiGate.WaitAsync();
            try
            {
                generatingWidgets.Set(_ => ImmutableHashSet<string>.Empty);
                List<string> failedCopy;
                lock (failedLock)
                    failedCopy = [..failed];
                genProgress.Set(new GenProgress(
                    "",
                    Volatile.Read(ref completed),
                    widgets.Count,
                    failedCopy,
                    false,
                    maxParallel));
                var finalFresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(finalFresh, revalidate: false);
                refreshToken.Refresh();
                queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
            }
            finally
            {
                uiGate.Release();
            }
        }

        void MarkGenerating(IEnumerable<string> widgetNames)
        {
            var names = widgetNames.Where(n => !string.IsNullOrEmpty(n)).ToHashSet();
            if (names.Count == 0) return;
            generatingWidgets.Set(s => s.Union(names).ToImmutableHashSet());
            refreshToken.Refresh();
        }

        void ShowFooterGenerateAllDialog()
        {
            showAlert(
                "Generate questions for all widgets that don't have questions yet?\n\nOpenAI will be called 3 times per widget (easy / medium / hard). The widget list loads after you tap OK; only widgets with no questions are generated.",
                result =>
                {
                    if (!result.IsOk()) return;
                    var cfgErr = QuestionGeneratorService.GetOpenAiConfigurationError(configuration);
                    if (cfgErr != null)
                    {
                        client.Toast(cfgErr);
                        return;
                    }

                    _ = RunGenerateAllAfterConfirmAsync();
                },
                "Generate All Questions",
                AlertButtonSet.OkCancel);
        }

        async Task RunGenerateAllAfterConfirmAsync()
        {
            try
            {
                var data = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(data, revalidate: false);
                refreshToken.Refresh();

                var allWidgets = data.Catalog;
                if (allWidgets.Count == 0)
                {
                    client.Toast("No widgets found. Check MCP docs or your database.");
                    return;
                }

                var notGenerated = allWidgets
                    .Where(w => !data.Rows.Any(r => r.Widget == w.Name && r.Easy + r.Medium + r.Hard > 0))
                    .ToList();

                if (notGenerated.Count == 0)
                {
                    client.Toast("Every widget already has at least one question.");
                    return;
                }

                MarkGenerating(notGenerated.Select(w => w.Name));
                genProgress.Set(new GenProgress("", 0, notGenerated.Count, [], true, WidgetGenerationParallelism));
                _ = GenerateBatchAsync(notGenerated);
            }
            catch (Exception ex)
            {
                client.Toast(ex.Message);
            }
        }

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
            return Layout.Vertical().Height(Size.Full())
                   | alertView
                   | TabLoadingSkeletons.QuestionsTab();

        var notGeneratedCount = baseRows.Count(r => r.Easy + r.Medium + r.Hard == 0);

        object progressBar;
        if (progress is { Active: true })
        {
            var pct = progress.Total > 0 ? progress.Done * 100 / progress.Total : 0;
            var statusLine = progress.MaxParallel > 1
                ? $"Completed {progress.Done}/{progress.Total} · up to {progress.MaxParallel} widgets in parallel"
                : progress.Total == 1
                    ? $"Generating questions for {progress.CurrentWidget}…"
                    : $"Generating {Math.Min(progress.Done + 1, progress.Total)}/{progress.Total}: {progress.CurrentWidget}…";
            progressBar = new Callout(
                Layout.Vertical()
                    | Text.Block(statusLine)
                    | new Progress(pct).Goal($"{progress.Done}/{progress.Total}"),
                variant: CalloutVariant.Info);
        }
        else if (progress is { Active: false, Total: > 0 })
        {
            var failCount = progress.Failed.Count;
            progressBar = new Callout(
                Layout.Horizontal()
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
                            var cfgErr = QuestionGeneratorService.GetOpenAiConfigurationError(configuration);
                            if (cfgErr != null)
                            {
                                client.Toast(cfgErr);
                                return;
                            }

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

    /// <summary>Clears <see cref="GenerateAllBridge.SetFlushHandler"/> when the Questions view unmounts.</summary>
    sealed class GenerateAllFlushRegistration : IDisposable
    {
        public void Dispose() => GenerateAllBridge.SetFlushHandler(null);
    }
}
