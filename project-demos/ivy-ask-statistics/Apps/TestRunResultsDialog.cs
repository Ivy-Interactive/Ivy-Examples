namespace IvyAskStatistics.Apps;

/// <summary>Modal listing all test results for a single test run, with edit/delete row actions.</summary>
internal sealed class TestRunResultsDialog(
    IState<bool> isOpen,
    Guid runId,
    IState<bool> editSheetOpen,
    IState<Guid> editQuestionId,
    IState<Guid?> editPreviewResultId) : ViewBase
{
    public override object? Build()
    {
        var factory      = UseService<AppDbContextFactory>();
        var client       = UseService<IClientProvider>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var (alertView, showAlert) = UseAlert();

        var resultsQuery = UseQuery<RunResultsPayload, string>(
            key: $"run-results-{runId}",
            fetcher: async (_, ct) =>
            {
                var payload = await LoadAsync(factory, runId, ct);
                refreshToken.Refresh();
                return payload;
            },
            options: new QueryOptions { KeepPrevious = true },
            tags: [("run-results", runId.ToString())]);

        var firstLoad = resultsQuery.Loading && resultsQuery.Value == null;
        var rows      = resultsQuery.Value?.Results ?? [];
        var runInfo   = resultsQuery.Value?.RunInfo;

        void Close() => isOpen.Set(false);

        async Task DeleteAsync(Guid resultId)
        {
            try
            {
                await using var ctx = factory.CreateDbContext();
                var entity = await ctx.TestResults.FirstOrDefaultAsync(r => r.Id == resultId);
                if (entity != null)
                {
                    ctx.TestResults.Remove(entity);
                    await ctx.SaveChangesAsync();

                    var run = await ctx.TestRuns.FirstOrDefaultAsync(r => r.Id == runId);
                    if (run != null)
                    {
                        var remaining = await ctx.TestResults
                            .Where(r => r.TestRunId == runId)
                            .ToListAsync();
                        run.TotalQuestions = remaining.Count;
                        run.SuccessCount   = remaining.Count(r => r.IsSuccess);
                        run.NoAnswerCount  = remaining.Count(r => !r.IsSuccess && r.HttpStatus == 404);
                        run.ErrorCount     = remaining.Count(r => !r.IsSuccess && r.HttpStatus != 404);
                        await ctx.SaveChangesAsync();
                    }
                }

                var updated = rows.Where(r => r.ResultId != resultId).ToList();
                resultsQuery.Mutator.Mutate(new RunResultsPayload(runInfo, updated), revalidate: false);
                refreshToken.Refresh();
                queryService.RevalidateByTag("dashboard-stats");
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
            body = new Callout("No results found for this run.", variant: CalloutVariant.Info);
        else
        {
            body = rows.AsQueryable()
                .ToDataTable(r => r.ResultId)
                .Key($"run-results-tbl-{runId}")
                .Height(Size.Units(120))
                .RefreshToken(refreshToken)
                .Header(r => r.Widget,        "Widget")
                .Header(r => r.Difficulty,    "Difficulty")
                .Header(r => r.Question,      "Question")
                .Header(r => r.Status,        "Status")
                .Header(r => r.Response,      "Response")
                .Header(r => r.ResponseTimeMs, "Time (ms)")
                .Width(r => r.Widget,         Size.Px(130))
                .Width(r => r.Difficulty,     Size.Px(80))
                .Width(r => r.Status,         Size.Px(90))
                .Width(r => r.ResponseTimeMs, Size.Px(80))
                .Width(r => r.Response,       Size.Px(300))
                .Hidden(r => r.ResultId)
                .Hidden(r => r.QuestionId)
                .AlignContent(r => r.ResponseTimeMs, Align.Left)
                .RowActions(
                    MenuItem.Default(Icons.Pencil, "edit").Label("Edit question").Tag("edit"),
                    MenuItem.Default(Icons.Trash2, "delete").Label("Delete result").Tag("delete"))
                .OnRowAction(e =>
                {
                    var args = e.Value;
                    var tag  = args?.Tag?.ToString();
                    if (!Guid.TryParse(args?.Id?.ToString(), out var resultId))
                        return ValueTask.CompletedTask;

                    var row = rows.FirstOrDefault(r => r.ResultId == resultId);
                    if (row == null) return ValueTask.CompletedTask;

                    if (tag == "edit")
                    {
                        editQuestionId.Set(row.QuestionId);
                        editPreviewResultId.Set(row.ResultId);
                        editSheetOpen.Set(true);
                    }
                    else if (tag == "delete")
                    {
                        var preview = row.Question.Length > 60 ? row.Question[..60] + "…" : row.Question;
                        showAlert(
                            $"Delete this result?\n\n\"{preview}\"",
                            async result =>
                            {
                                if (!result.IsOk()) return;
                                await DeleteAsync(resultId);
                            },
                            "Delete result",
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

        var title = firstLoad
            ? "Loading results…"
            : runInfo != null
                ? $"Results — {runInfo.IvyVersion} · {runInfo.Environment} ({rows.Count})"
                : $"Results ({rows.Count})";

        return new Fragment(
            alertView,
            new Dialog(
                onClose: _ => Close(),
                header: new DialogHeader(title),
                body: new DialogBody(body),
                footer: new DialogFooter(new Button("Close").OnClick(_ => Close())))
                .Width(Size.Units(320)));
    }

    private static async Task<RunResultsPayload> LoadAsync(
        AppDbContextFactory factory, Guid runId, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();

        var run     = await ctx.TestRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        var runInfo = run == null ? null : new RunInfo(run.IvyVersion ?? "", run.Environment ?? "production");

        var results = await ctx.TestResults.AsNoTracking()
            .Include(r => r.Question)
            .Where(r => r.TestRunId == runId)
            .OrderBy(r => r.Question.Widget)
            .ThenBy(r => r.Question.Difficulty)
            .Select(r => new RunResultRow(
                r.Id,
                r.QuestionId,
                r.Question.Widget,
                r.Question.Difficulty,
                r.Question.QuestionText,
                r.IsSuccess ? "answered" : r.HttpStatus == 404 ? "no answer" : "error",
                r.ResponseText,
                r.ResponseTimeMs))
            .ToListAsync(ct);

        return new RunResultsPayload(runInfo, results);
    }
}

internal record RunInfo(string IvyVersion, string Environment);
internal record RunResultsPayload(RunInfo? RunInfo, List<RunResultRow> Results);
internal record RunResultRow(
    Guid ResultId,
    Guid QuestionId,
    string Widget,
    string Difficulty,
    string Question,
    string Status,
    string Response,
    int ResponseTimeMs);
