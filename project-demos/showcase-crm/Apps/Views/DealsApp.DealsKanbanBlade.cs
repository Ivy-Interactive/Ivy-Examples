using Ivy.Hooks;
using Ivy.Views.Kanban;

namespace ShowcaseCrm.Apps.Views;

public class DealsKanbanBlade : ViewBase
{
    private record DealKanbanRecord(int Id, string CompanyName, string ContactName, decimal? Amount, string StageDescription, DateTime? CloseDate, string? LeadSource);

    private record DealTableRecord(int Id, string CompanyName, string ContactName, string Amount, string StageDescription, string CloseDate, string Lead);

    private static int StageOrder(string s) => s switch { "Prospecting" => 1, "Qualification" => 2, "Proposal" => 3, "Closed Won" => 4, "Closed Lost" => 5, _ => 0 };

    public override object? Build()
    {
        var refreshToken = UseRefreshToken();
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var (alertView, showAlert) = UseAlert();
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

        var tableData = data.Select(d => new DealTableRecord(
            d.Id,
            d.CompanyName,
            d.ContactName,
            d.Amount.HasValue ? d.Amount.Value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) : "N/A",
            d.StageDescription,
            d.CloseDate?.ToString("yyyy-MM-dd") ?? "—",
            d.LeadSource ?? "—"
        )).ToArray();

        var dataTableKey = $"deals-{data.Length}-{data.Aggregate(0, (h, d) => HashCode.Combine(h, d.Id, d.StageDescription, d.CloseDate, d.LeadSource))}";
        var dataTable = tableData.AsQueryable()
            .ToDataTable(idSelector: d => d.Id)
            .Key(dataTableKey)
            .Header(d => d.Id, "Id")
            .Header(d => d.CompanyName, "Company")
            .Header(d => d.ContactName, "Contact")
            .Header(d => d.Amount, "Amount")
            .Header(d => d.StageDescription, "Stage")
            .Header(d => d.CloseDate, "Date")
            .Header(d => d.Lead, "Lead")
            .Width(d => d.Id, Size.Px(40))
            .Width(d => d.CompanyName, Size.Px(250))
            .Width(d => d.Amount, Size.Px(100))
            .LoadAllRows(true)
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
            })
            .RowActions(
                MenuItem.Default(Icons.Pencil, "edit").Tag("edit"),
                MenuItem.Default(Icons.Trash2, "delete").Tag("delete")
            )
            .HandleRowAction(e =>
            {
                var args = e.Value;
                var tag = args.Tag?.ToString();
                var idStr = args.Id?.ToString();
                if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id)) return ValueTask.CompletedTask;

                if (tag == "edit")
                {
                    showSheet(id);
                }
                else if (tag == "delete")
                {
                    showAlert("Are you sure you want to delete this deal?", async result =>
                    {
                        if (result.IsOk())
                            await DeleteDeal(id, factory, queryService, () => { deals.Set([]); dealsQuery.Mutator.Revalidate(); });
                    }, "Delete Deal", AlertButtonSet.OkCancel);
                }
                return ValueTask.CompletedTask;
            })
            .Width(Size.Full())
            .Height(Size.Full());

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
            Layout.Vertical().Height(Size.Full())
                | Layout.Tabs(
                    new Tab("Kanban", kanban).Icon(Icons.LayoutGrid),
                    new Tab("DataTable", dataTable).Icon(Icons.Table)
                ).Variant(TabsVariant.Tabs).Height(Size.Fraction(1f)),
            sheetView,
            alertView
        );
    }

    private static async Task DeleteDeal(int id, ShowcaseCrmContextFactory factory, IQueryService queryService, Action revalidate)
    {
        await using var db = factory.CreateDbContext();
        var deal = await db.Deals.SingleOrDefaultAsync(d => d.Id == id);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag(typeof(Deal[]));
            revalidate();
        }
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
                    .Select(d => new DealKanbanRecord(d.Id, d.Company.Name, $"{d.Contact.FirstName} {d.Contact.LastName}", d.Amount, d.Stage.DescriptionText, d.CloseDate, d.Lead != null ? d.Lead.Source : null))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Deal[])],
            options: new QueryOptions { KeepPrevious = true });
    }
}
