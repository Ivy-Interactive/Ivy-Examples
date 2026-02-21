using Ivy.Hooks;
using Ivy.Views.Kanban;

namespace ShowcaseCrm.Apps.Views;

public class DealsKanbanBlade : ViewBase
{
    private record DealKanbanRecord(int Id, string CompanyName, string ContactName, decimal? Amount, string StageDescription);

    private static int StageOrder(string s) => s switch { "Prospecting" => 1, "Qualification" => 2, "Proposal" => 3, "Closed Won" => 4, "Closed Lost" => 5, _ => 0 };

    public override object? Build()
    {
        var refreshToken = UseRefreshToken();
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var dealsQuery = UseDealListRecords(Context);
        var deals = UseState<DealKanbanRecord[]>(() => []);

        var (sheetView, showSheet) = UseTrigger((IState<bool> isOpen, int id) => new DealEditSheet(isOpen, refreshToken, id));

        UseEffect(() =>
        {
            if (dealsQuery.Value != null && deals.Value.Length == 0)
                deals.Set(dealsQuery.Value);
        }, EffectTrigger.OnBuild());
        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                deals.Set([]);
                dealsQuery.Mutator.Revalidate();
            }
        }, [refreshToken]);

        if (dealsQuery.Value == null) return Text.Muted("Loading...");

        var data = deals.Value.Length > 0 ? deals.Value : dealsQuery.Value!;

        var kanban = data
            .ToKanban(
                groupBySelector: d => d.StageDescription,
                idSelector: d => d.Id.ToString(),
                orderSelector: d => d.Id)
            .CardBuilder(deal => new Card(
                content: deal.ToDetails()
                    .Remove(x => x.Id)
            )
            .HandleClick(() => showSheet(deal.Id)))
            .ColumnOrder<int>(d => StageOrder(d.StageDescription))
            .Width(Size.Full())
            .HandleMove(moveData =>
            {
                var cardId = moveData.CardId?.ToString();
                if (string.IsNullOrEmpty(cardId) || !int.TryParse(cardId, out int id)) return;

                var updatedTasks = data.ToList();
                var taskToMove = updatedTasks.FirstOrDefault(t => t.Id == id);
                if (taskToMove == null) return;

                var updated = taskToMove with { StageDescription = moveData.ToColumn };
                updatedTasks.RemoveAll(t => t.Id == id);

                int insertIndex = updatedTasks.Count;
                var taskAtTargetIndex = updatedTasks
                    .Where(t => t.StageDescription == moveData.ToColumn)
                    .ElementAtOrDefault(moveData.TargetIndex ?? -1);
                if (taskAtTargetIndex != null)
                    insertIndex = updatedTasks.IndexOf(taskAtTargetIndex);
                else
                {
                    var last = updatedTasks.LastOrDefault(t => t.StageDescription == moveData.ToColumn);
                    if (last != null) insertIndex = updatedTasks.IndexOf(last) + 1;
                }
                updatedTasks.Insert(insertIndex, updated);
                deals.Set(updatedTasks.ToArray());

                _ = MoveDeal(id, moveData.ToColumn, factory, queryService, () => dealsQuery.Mutator.Revalidate());
            })
            .Empty(
                new Card()
                    .Title("No Deals")
                    .Description("Create your first deal to get started")
            );

        return new Fragment(
            Layout.Vertical()
                | Icons.Plus.ToButton(_ => { }).Ghost().Tooltip("Create Deal").ToTrigger((o) => new DealCreateDialog(o, refreshToken))
                | kanban,
            sheetView
        );
    }

    private static async Task MoveDeal(int id, string toColumn, ShowcaseCrmContextFactory factory, IQueryService queryService, Action revalidate)
    {
        await using var db = factory.CreateDbContext();
        var stage = await db.DealStages.FirstOrDefaultAsync(s => s.DescriptionText == toColumn);
        if (stage == null) return;
        var deal = await db.Deals.FirstOrDefaultAsync(d => d.Id == id);
        if (deal == null) return;
        deal.StageId = stage.Id;
        deal.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        queryService.RevalidateByTag(typeof(Deal[]));
        revalidate();
    }

    private static QueryResult<DealKanbanRecord[]> UseDealListRecords(IViewContext context)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: nameof(DealsKanbanBlade),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals
                    .Include(d => d.Company).Include(d => d.Contact).Include(d => d.Stage)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(50)
                    .Select(d => new DealKanbanRecord(d.Id, d.Company.Name, $"{d.Contact.FirstName} {d.Contact.LastName}", d.Amount, d.Stage.DescriptionText))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Deal[])],
            options: new QueryOptions { KeepPrevious = true });
    }
}
