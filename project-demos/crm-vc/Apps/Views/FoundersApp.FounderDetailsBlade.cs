namespace Vc.Apps.Views;

public class FounderDetailsBlade(int founderId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var founder = UseState<Founder?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            founder.Set(await db.Founders
                .Include(f => f.Gender)
                .SingleOrDefaultAsync(f => f.Id == founderId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (founder.Value == null) return null;

        var founderValue = founder.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this founder?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Founder", AlertButtonSet.OkCancel);
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
            .ToTrigger((isOpen) => new FounderEditSheet(isOpen, refreshToken, founderId));

        var detailsCard = new Card(
            content: new
            {
                FullName = $"{founderValue.FirstName} {founderValue.LastName}",
                founderValue.Email,
                Gender = founderValue.Gender.DescriptionText
            }
            .ToDetails()
            .RemoveEmpty()
            .Builder(e => e.Email, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | dropDown
                | editBtn
        ).Title("Founder Details");

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(VcContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var founder = db.Founders.FirstOrDefault(f => f.Id == founderId)!;
        db.Founders.Remove(founder);
        db.SaveChanges();
    }
}