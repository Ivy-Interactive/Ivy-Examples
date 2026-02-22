namespace ShowcaseCrm.Apps.Views;

public class LeadDealsBlade(int? leadId) : ViewBase
{
    private record DealTableRecord(int Id, string Company, string Contact, string Stage, string Amount, string CloseDate);

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

        var dealsQuery = UseQuery(
            key: (nameof(LeadDealsBlade), leadId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals
                    .Include(e => e.Company)
                    .Include(e => e.Contact)
                    .Include(e => e.Stage)
                    .Where(e => e.LeadId == leadId)
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Deal[]), (typeof(Lead), leadId)]
        );

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                dealsQuery.Mutator.Revalidate();
                queryService.RevalidateByTag((typeof(Lead), leadId));
            }
        }, [refreshToken]);

        var (sheetView, showSheet) = UseTrigger((IState<bool> isOpen, int id) => new LeadDealsEditSheet(isOpen, refreshToken, id));

        if (dealsQuery.Loading) return Skeleton.Card();

        if (dealsQuery.Value == null || dealsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Deal").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new LeadDealsCreateDialog(isOpen, refreshToken, leadId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No deals found for this lead. Add a deal to get started.").Variant(CalloutVariant.Info);
        }

        var tableData = dealsQuery.Value.Select(d => new DealTableRecord(
            d.Id,
            d.Company.Name,
            $"{d.Contact.FirstName} {d.Contact.LastName}",
            d.Stage.DescriptionText,
            d.Amount.HasValue ? d.Amount.Value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) : "N/A",
            d.CloseDate?.ToString("yyyy-MM-dd") ?? "—"
        )).ToArray();

        var dataTableKey = $"deals-{leadId}-{tableData.Length}-{tableData.Aggregate(0, (h, d) => HashCode.Combine(h, d.Id))}";
        var dataTable = tableData.AsQueryable()
            .ToDataTable(idSelector: d => d.Id)
            .Key(dataTableKey)
            .Header(d => d.Id, "Id")
            .Header(d => d.Company, "Company")
            .Header(d => d.Contact, "Contact")
            .Header(d => d.Stage, "Stage")
            .Header(d => d.Amount, "Amount")
            .Header(d => d.CloseDate, "Date")
            .Width(d => d.Id, Size.Px(40))
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
                        {
                            await Delete(factory, id);
                            dealsQuery.Mutator.Revalidate();
                            queryService.RevalidateByTag((typeof(Lead), leadId));
                            refreshToken.Refresh();
                        }
                    }, "Delete Deal", AlertButtonSet.OkCancel);
                }
                return ValueTask.CompletedTask;
            })
            .Width(Size.Full())
            .Height(Size.Full());

        var addBtn = new Button("Add Deal").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new LeadDealsCreateDialog(isOpen, refreshToken, leadId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | dataTable
               | sheetView
               | alertView;
    }

    private async Task Delete(ShowcaseCrmContextFactory factory, int dealId)
    {
        await using var db = factory.CreateDbContext();
        var deal = await db.Deals.SingleOrDefaultAsync(e => e.Id == dealId);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            await db.SaveChangesAsync();
        }
    }
}
