namespace IvyAskStatistics.Apps;

/// <summary>Modal listing all DB questions for a single widget.</summary>
internal sealed class WidgetQuestionsDialog(IState<bool> isOpen, string widgetName) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AppDbContextFactory>();

        var tableQuery = UseQuery<List<QuestionDetailRow>, string>(
            key: widgetName,
            fetcher: async (name, ct) => await LoadQuestionsAsync(factory, name, ct),
            options: new QueryOptions { KeepPrevious = true });

        var firstLoad = tableQuery.Loading && tableQuery.Value == null;
        var rows      = tableQuery.Value ?? [];

        void Close() => isOpen.Set(false);

        object body;
        if (firstLoad)
        {
            body = Layout.Center()
                   | new Icon(Icons.Loader)
                   | Text.Muted("Loading…");
        }
        else if (rows.Count == 0)
        {
            body = new Callout(
                $"No questions in the database for \"{widgetName}\".",
                variant: CalloutVariant.Info);
        }
        else
        {
            body = QuestionsTable(rows, widgetName);
        }

        var title = firstLoad || rows.Count == 0
            ? $"Questions — {widgetName}"
            : $"Questions — {widgetName} ({rows.Count})";

        var footer = new DialogFooter(new Button("Close").OnClick(_ => Close()));

        return new Dialog(
            onClose: _ => Close(),
            header: new DialogHeader(title),
            body: new DialogBody(body),
            footer: footer)
            .Width(Size.Units(250));
    }

    private static object QuestionsTable(List<QuestionDetailRow> rows, string widgetName) =>
        rows.AsQueryable()
            .ToDataTable(r => r.Id)
            .Key($"widget-questions-{widgetName}")
            .Height(Size.Full())
            .Header(r => r.Difficulty,   "Difficulty")
            .Header(r => r.Category,     "Category")
            .Header(r => r.QuestionText, "Question")
            .Header(r => r.Source,       "Source")
            .Header(r => r.CreatedAt,    "Created")
            .Width(r => r.Difficulty,   Size.Px(90))
            .Width(r => r.Category,     Size.Px(140))
            .Width(r => r.Source,       Size.Px(90))
            .Width(r => r.CreatedAt,     Size.Px(170))
            .Config(c =>
            {
                c.AllowSorting    = true;
                c.AllowFiltering  = true;
                c.ShowSearch      = true;
                c.ShowIndexColumn = false;
            });

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
