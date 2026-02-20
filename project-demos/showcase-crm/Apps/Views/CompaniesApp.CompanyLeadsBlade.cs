namespace ShowcaseCrm.Apps.Views;

public class CompanyLeadsBlade(int? companyId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

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

        var table = leadsQuery.Value.Select(e => new
            {
                Status = e.Status.DescriptionText,
                Source = e.Source,
                CreatedAt = e.CreatedAt.ToString("yyyy-MM-dd"),
                UpdatedAt = e.UpdatedAt.ToString("yyyy-MM-dd"),
                _ = Layout.Horizontal().Gap(2)
                    | new Button("Delete", onClick: async _ =>
                        {
                            showAlert("Are you sure you want to delete this lead?", async result =>
                            {
                                if (result.IsOk())
                                {
                                    await Delete(factory, e.Id);
                                    leadsQuery.Mutator.Revalidate();
                                    queryService.RevalidateByTag((typeof(Company), companyId));
                                }
                            }, "Delete Lead", AlertButtonSet.OkCancel);
                        })
                        .Variant(ButtonVariant.Ghost)
                        .Icon(Icons.Trash)
                        .WithConfirm("Are you sure you want to delete this lead?", "Delete Lead")
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new CompanyLeadsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Lead").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CompanyLeadsCreateDialog(isOpen, refreshToken, companyId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
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