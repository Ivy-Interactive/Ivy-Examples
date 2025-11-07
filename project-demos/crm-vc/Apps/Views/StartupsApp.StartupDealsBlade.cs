namespace Vc.Apps.Views;

public class StartupDealsBlade(int startupId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var deals = this.UseState<Deal[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            deals.Set(await db.Deals.Include(e => e.Partners).Where(e => e.StartupId == startupId).ToArrayAsync());
        }, [ EffectTrigger.AfterInit(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this deal?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Deal", AlertButtonSet.OkCancel);
            };
        }

        if (deals.Value == null) return null;

        var table = deals.Value.Select(e => new
            {
                Round = e.Round,
                Amount = e.Amount,
                DealDate = e.DealDate,
                Partners = string.Join(", ", e.Partners.Select(p => $"{p.FirstName} {p.LastName}")),
                _ = Layout.Horizontal().Gap(1)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(e.Id)))
                    | Icons.Pencil
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new StartupDealsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Deal").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new StartupDealsCreateDialog(isOpen, refreshToken, startupId));

        return new Fragment()
               | BladeHelper.WithHeader(addBtn, table)
               | alertView;
    }

    public void Delete(VcContextFactory factory, int dealId)
    {
        using var db2 = factory.CreateDbContext();
        db2.Deals.Remove(db2.Deals.Single(e => e.Id == dealId));
        db2.SaveChanges();
    }
}