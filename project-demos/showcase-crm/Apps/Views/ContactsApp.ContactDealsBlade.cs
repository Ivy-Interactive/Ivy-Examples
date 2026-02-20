namespace ShowcaseCrm.Apps.Views;

public class ContactDealsBlade(int contactId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

        var dealsQuery = UseQuery(
            key: (nameof(ContactDealsBlade), contactId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals.Include(e => e.Stage).Where(e => e.ContactId == contactId).ToArrayAsync(ct);
            },
            tags: [typeof(Deal[]), (typeof(Contact), contactId)]
        );

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                dealsQuery.Mutator.Revalidate();
                queryService.RevalidateByTag((typeof(Contact), contactId));
            }
        }, [refreshToken]);

        if (dealsQuery.Loading) return Skeleton.Card();

        if (dealsQuery.Value == null || dealsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Deal").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new ContactDealsCreateDialog(isOpen, refreshToken, contactId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No deals found for this contact. Add a deal to get started.").Variant(CalloutVariant.Info);
        }

        var table = dealsQuery.Value.Select(e => new
            {
                Stage = e.Stage.DescriptionText,
                Amount = e.Amount,
                CloseDate = e.CloseDate,
                _ = Layout.Horizontal().Gap(2)
                    | new Button("Delete", onClick: OnDelete(e.Id))
                        .Variant(ButtonVariant.Ghost)
                        .Icon(Icons.Trash)
                        .WithConfirm("Are you sure you want to delete this deal?", "Delete Deal")
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new ContactDealsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Deal").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new ContactDealsCreateDialog(isOpen, refreshToken, contactId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;
    }

    private Action OnDelete(int id)
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var refreshToken = UseRefreshToken();

        return () =>
        {
            var (alertView, showAlert) = this.UseAlert();
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