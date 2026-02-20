namespace ShowcaseCrm.Apps.Views;

public class CompanyDealsBlade(int companyId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

        var dealsQuery = UseQuery(
            key: (nameof(CompanyDealsBlade), companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals
                    .Include(d => d.Contact)
                    .Include(d => d.Stage)
                    .Where(d => d.CompanyId == companyId)
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Deal[]), (typeof(Company), companyId)]
        );

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                dealsQuery.Mutator.Revalidate();
                queryService.RevalidateByTag((typeof(Company), companyId));
            }
        }, [refreshToken]);

        if (dealsQuery.Loading) return Skeleton.Card();

        if (dealsQuery.Value == null || dealsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Deal").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new CompanyDealsCreateDialog(isOpen, refreshToken, companyId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No deals found for this company. Add a deal to get started.").Variant(CalloutVariant.Info);
        }

        var table = dealsQuery.Value.Select(d => new
            {
                Contact = $"{d.Contact.FirstName} {d.Contact.LastName}",
                Stage = d.Stage.DescriptionText,
                Amount = d.Amount,
                CloseDate = d.CloseDate?.ToString("yyyy-MM-dd") ?? "N/A",
                _ = Layout.Horizontal().Gap(2)
                    | new Button("Delete", onClick: OnDelete(d.Id))
                        .Variant(ButtonVariant.Ghost)
                        .Icon(Icons.Trash)
                        .WithConfirm("Are you sure you want to delete this deal?", "Delete Deal")
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new CompanyDealsEditSheet(isOpen, refreshToken, d.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Deal").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CompanyDealsCreateDialog(isOpen, refreshToken, companyId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this deal?", async result =>
                {
                    if (result.IsOk())
                    {
                        await Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Deal", AlertButtonSet.OkCancel);
            };
        }
    }

    private async Task Delete(ShowcaseCrmContextFactory factory, int dealId)
    {
        await using var db = factory.CreateDbContext();
        var deal = await db.Deals.SingleOrDefaultAsync(d => d.Id == dealId);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            await db.SaveChangesAsync();
        }
    }
}