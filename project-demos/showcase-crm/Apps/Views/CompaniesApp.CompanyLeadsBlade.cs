namespace ShowcaseCrm.Apps.Views;

public class CompanyLeadsBlade(int? companyId) : ViewBase
{
    private record LeadTableRecord(int Id, string Status, string Source, string CreatedAt, string UpdatedAt);

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();
        var (sheetView, showSheet) = UseTrigger((IState<bool> isOpen, int id) => new CompanyLeadsEditSheet(isOpen, refreshToken, id));

        var leadsQuery = UseQuery(
            key: (nameof(CompanyLeadsBlade), companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads.Include(e => e.Status).Where(e => e.CompanyId == companyId).ToArrayAsync(ct);
            },
            tags: [typeof(Lead[]), (typeof(Company), companyId)]
        );

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                leadsQuery.Mutator.Revalidate();
                queryService.RevalidateByTag((typeof(Company), companyId));
            }
        }, [refreshToken]);

        if (leadsQuery.Loading) return Skeleton.Card();

        if (leadsQuery.Value == null || leadsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Lead").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new CompanyLeadsCreateDialog(isOpen, refreshToken, companyId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No leads found for this company. Add a lead to get started.").Variant(CalloutVariant.Info);
        }

        var tableData = leadsQuery.Value.Select(e => new LeadTableRecord(
            e.Id,
            e.Status.DescriptionText,
            e.Source ?? "",
            e.CreatedAt.ToString("yyyy-MM-dd"),
            e.UpdatedAt.ToString("yyyy-MM-dd")
        )).ToArray();
        var dataTableKey = $"leads-{companyId}-{tableData.Length}-{tableData.Aggregate(0, (h, l) => HashCode.Combine(h, l.Id))}";
        var dataTable = tableData.AsQueryable()
            .ToDataTable(idSelector: l => l.Id)
            .Key(dataTableKey)
            .Header(l => l.Id, "Id")
            .Header(l => l.Status, "Status")
            .Header(l => l.Source, "Source")
            .Header(l => l.CreatedAt, "Created")
            .Header(l => l.UpdatedAt, "Updated")
            .Width(l => l.Id, Size.Px(40))
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
                    showAlert("Are you sure you want to delete this lead?", async result =>
                    {
                        if (result.IsOk())
                        {
                            await Delete(factory, id);
                            leadsQuery.Mutator.Revalidate();
                            queryService.RevalidateByTag((typeof(Company), companyId));
                        }
                    }, "Delete Lead", AlertButtonSet.OkCancel);
                }
                return ValueTask.CompletedTask;
            })
            .Width(Size.Full())
            .Height(Size.Full());

        var addBtn = new Button("Add Lead").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CompanyLeadsCreateDialog(isOpen, refreshToken, companyId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | dataTable
               | sheetView
               | alertView;
    }

    private async Task Delete(ShowcaseCrmContextFactory factory, int leadId)
    {
        await using var db = factory.CreateDbContext();
        var lead = await db.Leads.SingleOrDefaultAsync(e => e.Id == leadId);
        if (lead != null)
        {
            db.Leads.Remove(lead);
            await db.SaveChangesAsync();
        }
    }
}