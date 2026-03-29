namespace IvyAskStatistics.Apps;

file record WidgetTableData(List<WidgetRow> Rows, List<IvyWidget> Catalog, int QueryKey);

[App(icon: Icons.Database, title: "Questions")]
public class QuestionsApp : ViewBase
{
    public override object? Build()
    {
        var factory       = UseService<AppDbContextFactory>();
        var configuration = UseService<IConfiguration>();
        var client        = UseService<IClientProvider>();

        var generateRequest   = UseState<IvyWidget?>(null);
        var generatingWidget  = UseState("");
        var generatingStatus  = UseState("");
        var refreshTick       = UseState(0);
        var pendingRefreshFor = UseState(""); // widget whose table row is reloading after generation
        var refreshToken      = UseRefreshToken();
        var (alertView, showAlert) = UseAlert();

        var tableQuery = UseQuery<WidgetTableData, int>(
            key: refreshTick.Value,
            fetcher: async (queryKey, ct) =>
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
                catch { /* offline */ }

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
            },
            options: new QueryOptions { KeepPrevious = true });

        // Clear "Updating…" once the query result matches the current refreshTick (KeepPrevious-safe)
        UseEffect(async () =>
        {
            if (string.IsNullOrEmpty(pendingRefreshFor.Value)) return;
            var targetKey = refreshTick.Value;
            for (var guard = 0; guard < 600; guard++)
            {
                if (tableQuery.Value?.QueryKey == targetKey && !tableQuery.Loading)
                    break;
                await Task.Delay(16);
            }
            if (!string.IsNullOrEmpty(pendingRefreshFor.Value))
            {
                pendingRefreshFor.Set("");
                refreshToken.Refresh();
            }
        }, [refreshTick.ToTrigger()]);

        UseEffect(() => { refreshToken.Refresh(); },
            [generatingWidget.ToTrigger(), generatingStatus.ToTrigger()]);

        UseEffect(async () =>
        {
            var widget = generateRequest.Value;
            if (widget == null) return;

            generatingWidget.Set(widget.Name);
            try
            {
                var baseUrl = configuration["IvyAsk:BaseUrl"] ?? IvyAskService.DefaultMcpBaseUrl;
                await QuestionGeneratorService.GenerateAndSaveAsync(
                    widget, factory, baseUrl,
                    new Progress<string>(msg => generatingStatus.Set(msg)));

                pendingRefreshFor.Set(widget.Name);
                refreshTick.Set(refreshTick.Value + 1);
                client.Toast($"Generated 30 questions for {widget.Name}");
            }
            catch (Exception ex)
            {
                client.Toast($"Error: {ex.Message}");
                pendingRefreshFor.Set("");
            }
            finally
            {
                generatingWidget.Set("");
                generatingStatus.Set("");
                generateRequest.Set(null);
            }
        }, [generateRequest.ToTrigger()]);

        // ── Derived state ─────────────────────────────────────────────────────
        var baseRows     = tableQuery.Value?.Rows    ?? [];
        var catalog      = tableQuery.Value?.Catalog ?? [];
        var generating   = generatingWidget.Value;
        var isGenerating = !string.IsNullOrEmpty(generating);
        // Only the initial mount uses the full-screen loader. After refreshTick bumps (post-generation
        // refetch), never swap the whole view for Loading — avoids the table vanishing mid-flow.
        var firstLoad    = tableQuery.Loading && tableQuery.Value == null && refreshTick.Value == 0;
        var pendingRow   = pendingRefreshFor.Value;

        static string IdleStatus(WidgetRow r)
        {
            var n = r.Easy + r.Medium + r.Hard;
            return n == 0 ? "○ Not generated" : "✓ Generated";
        }

        // Status per row: generating / updating DB / idle
        var rows = baseRows.Select(r =>
        {
            if (r.Widget == generating)
            {
                return r with { Status = $"Generating…" };
            }

            if (r.Widget == pendingRow)
                return r with { Status = "Updating…" };

            return r with { Status = IdleStatus(r) };
        }).ToList();

        if (firstLoad)
            return Layout.Center()
                   | new Icon(Icons.Loader)
                   | Text.Muted("Loading…");

        var table = rows.AsQueryable()
            .ToDataTable(r => r.Widget)
            .RefreshToken(refreshToken)
            // Stable key: do not tie to refreshTick — that remounted the whole table after each generation.
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
                MenuItem.Default(Icons.Sparkles, "generate").Label("Generate questions").Tag("generate"))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args?.Tag?.ToString() != "generate") return ValueTask.CompletedTask;
                if (isGenerating)
                {
                    client.Toast("Already generating — please wait");
                    return ValueTask.CompletedTask;
                }
                var name   = args.Id?.ToString() ?? "";
                var widget = catalog.FirstOrDefault(w => w.Name == name)
                    ?? new IvyWidget(name, rows.FirstOrDefault(r => r.Widget == name)?.Category ?? "", "");
                showAlert(
                    $"Generate 30 questions for the \"{widget.Name}\" widget?\n\nIvy Ask will be called three times (easy / medium / hard). Any previously generated questions for this widget will be replaced.",
                    result =>
                    {
                        if (!result.IsOk()) return;
                        // Same pattern as pr-staging-deploy: update UI immediately on confirm, then run async work.
                        generatingWidget.Set(widget.Name);
                        generatingStatus.Set("Starting…");
                        generateRequest.Set(widget);
                        refreshToken.Refresh();
                    },
                    "Generate questions",
                    AlertButtonSet.OkCancel);
                return ValueTask.CompletedTask;
            })
            .Config(config =>
            {
                config.AllowSorting    = true;
                config.AllowFiltering  = true;
                config.ShowSearch      = true;
                config.ShowIndexColumn = false;
            });

        return Layout.Vertical().Height(Size.Full())
               | alertView
               | table;
    }
}
