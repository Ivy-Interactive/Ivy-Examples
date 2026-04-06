namespace IvyAskStatistics.Apps;

internal sealed class QuestionEditSheet(
    IState<bool> isOpen,
    Guid questionId,
    IState<Guid?> previewResultId) : ViewBase
{
    private record EditRequest
    {
        [Required]
        public string QuestionText { get; init; } = "";

        [Required]
        public string Difficulty { get; init; } = "";

        public string Category { get; init; } = "";

        public bool IsActive { get; init; } = true;
    }

    /// <summary>
    /// Question row plus optional response preview: either the specific <see cref="TestRunResultEntity"/> row
    /// (when opened from a run results table) or the latest answer across all runs (elsewhere).
    /// </summary>
    private sealed record QuestionEditPayload(
        QuestionEntity Question,
        string? AnswerText,
        AnswerPreviewSource PreviewSource);

    private enum AnswerPreviewSource
    {
        /// <summary>This run's result row (may be empty).</summary>
        ThisResultRow,

        /// <summary>Latest successful, else latest with text — any run.</summary>
        GlobalHistory,
    }

    public override object? Build()
    {
        var factory      = UseService<AppDbContextFactory>();
        var queryService = UseService<IQueryService>();
        var isSaving     = UseState(false);

        var questionQuery = UseQuery<QuestionEditPayload?, (Guid QuestionId, Guid? PreviewResultId)>(
            key: (questionId, previewResultId.Value),
            fetcher: async (key, ct) =>
            {
                var (id, focusResult) = key;
                await using var ctx = factory.CreateDbContext();
                var q = await ctx.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
                if (q == null)
                    return null;

                if (focusResult is Guid fr && fr != Guid.Empty)
                {
                    var text = await ctx.TestResults.AsNoTracking()
                        .Where(r => r.Id == fr && r.QuestionId == id)
                        .Select(r => r.ResponseText)
                        .FirstOrDefaultAsync(ct);
                    var trimmed = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                    return new QuestionEditPayload(q, trimmed, AnswerPreviewSource.ThisResultRow);
                }

                var lastSuccess = await ctx.TestResults.AsNoTracking()
                    .Where(r => r.QuestionId == id && r.IsSuccess)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.ResponseText)
                    .FirstOrDefaultAsync(ct);

                string? answer = null;
                if (!string.IsNullOrWhiteSpace(lastSuccess))
                    answer = lastSuccess.Trim();
                else
                {
                    var lastAny = await ctx.TestResults.AsNoTracking()
                        .Where(r => r.QuestionId == id)
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => r.ResponseText)
                        .FirstOrDefaultAsync(ct);
                    if (!string.IsNullOrWhiteSpace(lastAny))
                        answer = lastAny.Trim();
                }

                return new QuestionEditPayload(q, answer, AnswerPreviewSource.GlobalHistory);
            });

        if (questionQuery.Loading || questionQuery.Value == null)
            return new Sheet(
                    _ =>
                    {
                        isOpen.Set(false);
                        previewResultId.Set(null);
                    },
                    Skeleton.Form(),
                    title: "Edit Question",
                    description: "Loading question…")
                .Width(Size.Fraction(1f / 3f))
                .Height(Size.Full());

        var payload = questionQuery.Value;
        var q       = payload.Question;
        var answer  = payload.AnswerText;
        var preview = payload.PreviewSource;

        var form = new EditRequest
        {
            QuestionText = q.QuestionText ?? "",
            Difficulty   = q.Difficulty,
            Category     = q.Category,
            IsActive     = q.IsActive,
        };

        var difficulties = new[] { "easy", "medium", "hard" }.ToOptions();

        var formBuilder = form
            .ToForm()
            .Builder(f => f.QuestionText, f => f.ToTextareaInput())
            .Builder(f => f.Difficulty,   f => f.ToSelectInput(difficulties))
            .Builder(f => f.Category,     f => f.ToTextInput())
            .Builder(f => f.IsActive,     f => f.ToSwitchInput())
            .OnSubmit(OnSubmit);

        var (onSubmit, formView, validationView, loading) = formBuilder.UseForm(Context);

        object answerPreview = preview switch
        {
            AnswerPreviewSource.ThisResultRow when string.IsNullOrWhiteSpace(answer)
                => new Callout(
                    "No response text for this row in this test run (e.g. 404 / no answer).",
                    variant: CalloutVariant.Info),
            AnswerPreviewSource.ThisResultRow
                => new Card(
                        Layout.Vertical().Gap(2)
                            | Text.Block("Response for the result row you opened (read-only).").Muted()
                            | Text.Markdown(answer!))
                    .Title("This run")
                    .Icon(Icons.FileText),
            AnswerPreviewSource.GlobalHistory when string.IsNullOrWhiteSpace(answer)
                => new Empty(),
            AnswerPreviewSource.GlobalHistory
                => new Card(
                        Layout.Vertical().Gap(2)
                            | Text.Block("Latest recorded response across all test runs (read-only).").Muted()
                            | Text.Markdown(answer!))
                    .Title("Answer")
                    .Icon(Icons.FileText),
            _ => new Empty()
        };

        var scrollBody = Layout.Vertical().Gap(4)
            | formView
            | answerPreview;

        var footer = Layout.Horizontal().Gap(2)
            | new Button("Save")
                .Variant(ButtonVariant.Primary)
                .Loading(loading || isSaving.Value)
                .Disabled(loading || isSaving.Value)
                .OnClick(async _ =>
                {
                    isSaving.Set(true);
                    try
                    {
                        if (await onSubmit())
                            isOpen.Set(false);
                    }
                    finally
                    {
                        isSaving.Set(false);
                    }
                })
            | validationView;

        var sheetBody = new FooterLayout(footer, scrollBody);

        return new Sheet(
                _ =>
                {
                    isOpen.Set(false);
                    previewResultId.Set(null);
                },
                sheetBody,
                title: "Edit Question")
            .Width(Size.Fraction(1f / 3f))
            .Height(Size.Full());

        async Task OnSubmit(EditRequest? request)
        {
            if (request == null) return;
            await using var ctx = factory.CreateDbContext();
            var entity = await ctx.Questions.FirstOrDefaultAsync(e => e.Id == questionId);
            if (entity == null) return;
            entity.QuestionText = request.QuestionText.Trim();
            entity.Difficulty   = request.Difficulty;
            entity.Category     = request.Category.Trim();
            entity.IsActive     = request.IsActive;
            await ctx.SaveChangesAsync();
            queryService.RevalidateByTag(("widget-questions", entity.Widget));
            queryService.RevalidateByTag("widget-summary");
            queryService.RevalidateByTag(RunApp.TestQuestionsQueryTag);
        }
    }
}
