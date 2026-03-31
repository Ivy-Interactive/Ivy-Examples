using System.Collections.Immutable;

namespace IvyAskStatistics.Apps;

internal sealed record WidgetTableData(List<WidgetRow> Rows, List<IvyWidget> Catalog, int QueryKey);

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
        var (alertView, showAlert) = UseAlert();

        var tableQuery = UseQuery<WidgetTableData, int>(
            key: TableQueryKey,
            fetcher: async (qk, ct) =>
            {
                var result = await LoadWidgetTableDataAsync(factory, qk, ct);
                refreshToken.Refresh();
                return result;
            },
            options: new QueryOptions { KeepPrevious = true },
            tags: ["widget-summary"]);

        UseEffect(async () =>
        {
            var widgetName = deleteRequest.Value;
            if (string.IsNullOrEmpty(widgetName)) return;

            try
            {
                await using var ctx = factory.CreateDbContext();
                var list = await ctx.Questions.Where(q => q.Widget == widgetName).ToListAsync();
                if (list.Count == 0)
                {
                    client.Toast("No questions to delete.");
                    return;
                }
                ctx.Questions.RemoveRange(list);
                await ctx.SaveChangesAsync();

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                refreshToken.Refresh();
                client.Toast($"Deleted {list.Count} question(s) for \"{widgetName}\".");
            }
            catch (Exception ex)
            {
                client.Toast($"Error: {ex.Message}");
            }
            finally
            {
                deleteRequest.Set(null);
            }
        }, [deleteRequest.ToTrigger()]);

        async Task GenerateWidgetAsync(IvyWidget widget)
        {
            try
            {
                var apiKey  = configuration["OpenAI:ApiKey"]  ?? throw new InvalidOperationException("OpenAI:ApiKey secret is not set.");
                var baseUrl = configuration["OpenAI:BaseUrl"] ?? throw new InvalidOperationException("OpenAI:BaseUrl secret is not set.");
                await QuestionGeneratorService.GenerateAndSaveAsync(widget, factory, apiKey, baseUrl);

                var fresh = await LoadWidgetTableDataAsync(factory, TableQueryKey, CancellationToken.None);
                tableQuery.Mutator.Mutate(fresh, revalidate: false);
                refreshToken.Refresh();
                client.Toast($"Generated 30 questions for {widget.Name}");
            }
            catch (Exception ex)
            {
                client.Toast($"Error ({widget.Name}): {ex.Message}");
            }
            finally
            {
                generatingWidgets.Set(s => s.Remove(widget.Name));
                refreshToken.Refresh();
            }
        }

        void MarkGenerating(IEnumerable<string> widgetNames)
        {
            var names = widgetNames.Where(n => !string.IsNullOrEmpty(n)).ToHashSet();
            if (names.Count == 0) return;

            generatingWidgets.Set(s => s.Union(names).ToImmutableHashSet());
            refreshToken.Refresh();
        }

        var generating = generatingWidgets.Value;
        var baseRows   = tableQuery.Value?.Rows ?? [];
        var catalog    = tableQuery.Value?.Catalog ?? [];
        var isDeleting = !string.IsNullOrEmpty(deleteRequest.Value);
        var firstLoad  = tableQuery.Loading && tableQuery.Value == null;

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
                    if (isDeleting)
                    {
                        client.Toast("Please wait — delete in progress");
                        return ValueTask.CompletedTask;
                    }

                    var genName = args.Id?.ToString() ?? "";
                    if (generating.Contains(genName))
                    {
                        client.Toast($"\"{genName}\" is already being generated");
                        return ValueTask.CompletedTask;
                    }

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
                    if (isDeleting)
                    {
                        client.Toast("Delete already in progress — please wait");
                        return ValueTask.CompletedTask;
                    }

                    var delName = args.Id?.ToString() ?? "";
                    if (generating.Contains(delName))
                    {
                        client.Toast($"\"{delName}\" is currently being generated — please wait");
                        return ValueTask.CompletedTask;
                    }

                    var row = rows.FirstOrDefault(r => r.Widget == delName);
                    var n   = row == null ? 0 : row.Easy + row.Medium + row.Hard;
                    if (n == 0)
                    {
                        client.Toast("No questions to delete for this widget.");
                        return ValueTask.CompletedTask;
                    }

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
