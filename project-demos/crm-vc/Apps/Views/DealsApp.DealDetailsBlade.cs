namespace Vc.Apps.Views;

public class DealDetailsBlade(int dealId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var deal = UseState<Deal?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            deal.Set(await db.Deals.Include(d => d.Startup).SingleOrDefaultAsync(d => d.Id == dealId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (deal.Value == null) return null;

        var dealValue = deal.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this deal?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Deal", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(onDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new DealEditSheet(isOpen, refreshToken, dealId));

        var detailsCard = new Card(
            content: new
            {
                Id = dealValue.Id,
                StartupName = dealValue.Startup.Name,
                Round = dealValue.Round,
                Amount = dealValue.Amount,
                DealDate = dealValue.DealDate?.ToString("yyyy-MM-dd")
            }
            .ToDetails()
            .RemoveEmpty()
            .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Deal Details");

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(VcContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var deal = db.Deals.FirstOrDefault(d => d.Id == dealId)!;
        db.Deals.Remove(deal);
        db.SaveChanges();
    }
}