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
        var deals = UseDealListRecords(Context);

        var (sheetView, showSheet) = UseTrigger((IState<bool> isOpen, int id) => new DealEditSheet(isOpen, refreshToken, id));

        UseEffect(() => { if (refreshToken.ReturnValue is int) deals.Mutator.Revalidate(); }, [refreshToken]);

        if (deals.Value == null) return Text.Muted("Loading...");

        var kanban = deals.Value
            .ToKanban(
                groupBySelector: d => d.StageDescription,
                idSelector: d => d.Id,
                orderSelector: d => d.Id)
            .CardBuilder(deal => new Card(
                content: deal.ToDetails()
                    .Remove(x => x.Id)
            )
            .HandleClick(() => showSheet(deal.Id)))
            .ColumnOrder<int>(d => StageOrder(d.StageDescription))
            .Width(Size.Full())
            .HandleMove(moveData => _ = MoveDeal(moveData, factory, queryService, () => deals.Mutator.Revalidate()))
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

    private static async Task MoveDeal((object? CardId, string ToColumn, int? TargetIndex) moveData, ShowcaseCrmContextFactory factory, IQueryService queryService, Action revalidate)
    {
        if (moveData.CardId == null || !int.TryParse(moveData.CardId.ToString(), out int id)) return;
        await using var db = factory.CreateDbContext();
        var stage = await db.DealStages.FirstOrDefaultAsync(s => s.DescriptionText == moveData.ToColumn);
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
