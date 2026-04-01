namespace IvyAskStatistics.Apps;

internal sealed class QuestionEditSheet(IState<bool> isOpen, Guid questionId) : ViewBase
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

    public override object? Build()
    {
        var factory      = UseService<AppDbContextFactory>();
        var queryService = UseService<IQueryService>();

        var questionQuery = UseQuery<QuestionEntity?, Guid>(
            key: questionId,
            fetcher: async (id, ct) =>
            {
                await using var ctx = factory.CreateDbContext();
                return await ctx.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, ct);
            });

        if (questionQuery.Loading || questionQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Question");

        var q = questionQuery.Value;

        var form = new EditRequest
        {
            QuestionText = q.QuestionText ?? "",
            Difficulty   = q.Difficulty,
            Category     = q.Category,
            IsActive     = q.IsActive,
        };

        var difficulties = new[] { "easy", "medium", "hard" }.ToOptions();

        return form
            .ToForm()
            .Builder(f => f.QuestionText, f => f.ToTextareaInput())
            .Builder(f => f.Difficulty,   f => f.ToSelectInput(difficulties))
            .OnSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Question");

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
            isOpen.Set(false);
        }
    }
}
