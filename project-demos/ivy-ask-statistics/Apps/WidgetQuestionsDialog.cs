namespace IvyAskStatistics.Apps;

/// <summary>Modal listing all DB questions for a single widget.</summary>
internal sealed class WidgetQuestionsDialog(
    IState<bool> isOpen,
    string widgetName,
    IState<bool> editSheetOpen,
    IState<Guid> editQuestionId,
    IState<Guid?> editPreviewResultId) : ViewBase
{
    public override object? Build()
    {
        var factory       = UseService<AppDbContextFactory>();
        var client        = UseService<IClientProvider>();
        var queryService  = UseService<IQueryService>();
        var refreshToken  = UseRefreshToken();

        var (alertView, showAlert) = UseAlert();

        var tableQuery = UseQuery<List<QuestionDetailRow>, string>(
            key: widgetName,
            fetcher: async (name, ct) =>
            {
                var result = await LoadQuestionsAsync(factory, name, ct);
                refreshToken.Refresh();
                return result;
            },
            options: new QueryOptions { KeepPrevious = true },
            tags: [("widget-questions", widgetName)]);

        var firstLoad = tableQuery.Loading && tableQuery.Value == null;
        var rows      = tableQuery.Value ?? [];

        void Close() => isOpen.Set(false);

        async Task DeleteAsync(Guid id)
        {
            try
            {
                await using var ctx = factory.CreateDbContext();
                var entity = await ctx.Questions.FirstOrDefaultAsync(q => q.Id == id);
                if (entity != null)
                {
                    ctx.Questions.Remove(entity);
                    await ctx.SaveChangesAsync();
                }
                var updated = rows.Where(r => r.Id != id).ToList();
                tableQuery.Mutator.Mutate(updated, revalidate: false);
                refreshToken.Refresh();
                queryService.RevalidateByTag("widget-summary");
                queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
            }
            catch (Exception ex)
            {
                client.Toast($"Error: {ex.Message}");
            }
        }

        object body;
        if (firstLoad)
            body = TabLoadingSkeletons.DialogTable();
        else if (rows.Count == 0)
        {
            body = new Callout(
                $"No questions in the database for \"{widgetName}\".",
                variant: CalloutVariant.Info);
        }
        else
        {
            body = rows.AsQueryable()
                .ToDataTable(r => r.Id)
                .Key($"widget-questions-{widgetName}")
                .Height(Size.Units(120))
                .RefreshToken(refreshToken)
                .Header(r => r.Difficulty,   "Difficulty")
                .Header(r => r.Category,     "Category")
                .Header(r => r.QuestionText, "Question")
                .Header(r => r.Source,       "Source")
                .Header(r => r.CreatedAt,    "Created")
                .Width(r => r.Difficulty,    Size.Px(80))
                .Width(r => r.QuestionText,  Size.Px(340))
                .Width(r => r.Source,        Size.Px(100))
                .Width(r => r.CreatedAt,     Size.Px(170))
                .Hidden(r => r.Id)
                .RowActions(
                    MenuItem.Default(Icons.Pencil, "edit").Label("Edit").Tag("edit"),
                    MenuItem.Default(Icons.Trash2, "delete").Label("Delete").Tag("delete"))
                .OnRowAction(e =>
                {
                    var args = e.Value;
                    var tag  = args?.Tag?.ToString();
                    if (!Guid.TryParse(args?.Id?.ToString(), out var id)) return ValueTask.CompletedTask;

                    if (tag == "edit")
                    {
                        editPreviewResultId.Set(null);
                        editQuestionId.Set(id);
                        editSheetOpen.Set(true);
                    }
                    else if (tag == "delete")
                    {
                        var text    = rows.FirstOrDefault(r => r.Id == id)?.QuestionText ?? "";
                        var preview = text.Length > 60 ? text[..60] + "…" : text;
                        showAlert(
                            $"Delete this question?\n\n\"{preview}\"",
                            async result =>
                            {
                                if (!result.IsOk()) return;
                                await DeleteAsync(id);
                            },
                            "Delete question",
                            AlertButtonSet.OkCancel);
                    }

                    return ValueTask.CompletedTask;
                })
                .Config(c =>
                {
                    c.AllowSorting    = true;
                    c.AllowFiltering  = true;
                    c.ShowSearch      = true;
                    c.ShowIndexColumn = false;
                });
        }

        var title = firstLoad || rows.Count == 0
            ? $"Questions — {widgetName}"
            : $"Questions — {widgetName} ({rows.Count})";

        var footer = new DialogFooter(new Button("Close").OnClick(_ => Close()));

        return new Fragment(
            alertView,
            new Dialog(
                onClose: _ => Close(),
                header: new DialogHeader(title),
                body: new DialogBody(body),
                footer: footer)
                .Width(Size.Units(240)));
    }

    private static async Task<List<QuestionDetailRow>> LoadQuestionsAsync(
        AppDbContextFactory factory,
        string name,
        CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        return await ctx.Questions
            .AsNoTracking()
            .Where(q => q.Widget == name)
            .OrderBy(q => q.Difficulty)
            .ThenBy(q => q.Category)
            .ThenBy(q => q.CreatedAt)
            .Select(q => new QuestionDetailRow(
                q.Id,
                q.Difficulty,
                q.Category,
                q.QuestionText ?? "",
                q.Source,
                q.CreatedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm", CultureInfo.CurrentCulture)))
            .ToListAsync(ct);
    }
}
