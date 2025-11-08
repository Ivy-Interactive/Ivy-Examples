namespace Vc.Apps.Views;

public class StartupDetailsBlade(int startupId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<VcContextFactory>();
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var startup = this.UseState<Startup?>();
        var dealCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            startup.Set(await db.Startups.SingleOrDefaultAsync(e => e.Id == startupId));
            dealCount.Set(await db.Deals.CountAsync(e => e.StartupId == startupId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (startup.Value == null) return null;

        var startupValue = startup.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this startup?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Startup", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new StartupEditSheet(isOpen, refreshToken, startupId));

        var detailsCard = new Card(
            content: new
                {
                    startupValue.Id,
                    startupValue.Name,
                    startupValue.Description,
                    FoundedAt = startupValue.FoundedAt?.ToString("yyyy-MM-dd")
                }.ToDetails()
                .RemoveEmpty()
                .MultiLine(e => e.Description)
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                    | dropDown
                    | editBtn
        )
            .Width(Size.Fit().Max(100))
            .Title("Startup Details");

        var relatedCard = new Card(
            new List(
                new ListItem("Deals", onClick: _ =>
                {
                    blades.Push(this, new StartupDealsBlade(startupId), "Deals");
                }, badge: dealCount.Value.ToString("N0"))
            ));

        return new Fragment()
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(VcContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var startup = db.Startups.FirstOrDefault(e => e.Id == startupId)!;
        db.Startups.Remove(startup);
        db.SaveChanges();
    }
}