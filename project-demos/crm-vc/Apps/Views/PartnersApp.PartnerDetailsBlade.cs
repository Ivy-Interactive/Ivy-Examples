namespace Vc.Apps.Views;

public class PartnerDetailsBlade(int partnerId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var partner = UseState<Partner?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            partner.Set(await db.Partners.Include(p => p.Gender).SingleOrDefaultAsync(p => p.Id == partnerId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (partner.Value == null) return null;

        var partnerValue = partner.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this partner?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Partner", AlertButtonSet.OkCancel);
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
            .ToTrigger((isOpen) => new PartnerEditSheet(isOpen, refreshToken, partnerId));

        var detailsCard = new Card(
            content: new
            {
                FullName = $"{partnerValue.FirstName} {partnerValue.LastName}",
                Email = partnerValue.Email,
                Gender = partnerValue.Gender.DescriptionText
            }
            .ToDetails()
            .RemoveEmpty()
            .Builder(e => e.Email, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | dropDown
                | editBtn
        ).Title("Partner Details");

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(VcContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var partner = db.Partners.FirstOrDefault(p => p.Id == partnerId)!;
        db.Partners.Remove(partner);
        db.SaveChanges();
    }
}