namespace ShowcaseCrm.Apps.Views;

public class LeadDealsBlade(int? leadId) : ViewBase
{
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

        if (dealsQuery.Loading) return Skeleton.Card();

        if (dealsQuery.Value == null || dealsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Deal").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new LeadDealsCreateDialog(isOpen, refreshToken, leadId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No deals found for this lead. Add a deal to get started.").Variant(CalloutVariant.Info);
        }

        var table = dealsQuery.Value.Select(e => new
            {
                Company = e.Company.Name,
                Contact = $"{e.Contact.FirstName} {e.Contact.LastName}",
                Stage = e.Stage.DescriptionText,
                Amount = e.Amount,
                CloseDate = e.CloseDate?.ToString("yyyy-MM-dd"),
                _ = Layout.Horizontal().Gap(2)
                    | new Button("Delete", onClick: async _ =>
                        {
                            showAlert("Are you sure you want to delete this deal?", async result =>
                            {
                                if (result.IsOk())
                                {
                                    await Delete(factory, e.Id);
                                    dealsQuery.Mutator.Revalidate();
                                    queryService.RevalidateByTag((typeof(Lead), leadId));
                                }
                            }, "Delete Deal", AlertButtonSet.OkCancel);
                        })
                        .Variant(ButtonVariant.Ghost)
                        .Icon(Icons.Trash)
                        .WithConfirm("Are you sure you want to delete this deal?", "Delete Deal")
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new LeadDealsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Deal").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new LeadDealsCreateDialog(isOpen, refreshToken, leadId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
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