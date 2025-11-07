namespace Vc.Apps.Views;

public class IndustryDetailsBlade(int industryId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var industry = UseState<Industry?>(() => null!);
        var startups = UseState<List<Startup>>(() => new());
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            industry.Set(await db.Industries.SingleOrDefaultAsync(e => e.Id == industryId));
            startups.Set(await db.Startups
                .Where(s => s.Industries.Any(i => i.Id == industryId))
                .ToListAsync());
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (industry.Value == null) return null;

        var industryValue = industry.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this industry?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Industry", AlertButtonSet.OkCancel);
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
            .ToTrigger((isOpen) => new IndustryEditSheet(isOpen, refreshToken, industryId));

        var detailsCard = new Card(
            content: new
            {
                Id = industryValue.Id,
                Name = industryValue.Name,
                StartupCount = startups.Value.Count
            }
            .ToDetails()
            .RemoveEmpty()
            .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | dropDown
                | editBtn
        ).Title("Industry Details");

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(VcContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var industry = db.Industries.FirstOrDefault(e => e.Id == industryId)!;
        db.Industries.Remove(industry);
        db.SaveChanges();
    }
}