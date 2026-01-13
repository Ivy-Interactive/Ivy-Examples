namespace AutodealerCrm.Apps.Views;

public class MediumDetailsBlade(int mediumId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<AutodealerCrmContextFactory>();
        var blades = this.UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();
        var medium = this.UseState<Medium?>();
        var messageCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            medium.Set(await db.Media
                .Include(e => e.Customer)
                .Include(e => e.Lead)
                .Include(e => e.Vehicle)
                .SingleOrDefaultAsync(e => e.Id == mediumId));
            messageCount.Set(await db.Messages.CountAsync(e => e.MediaId == mediumId));
        }, [EffectTrigger.OnMount(), refreshToken]);

        if (medium.Value == null) return null;

        var mediumValue = medium.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this media?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Media", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete)
            );

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new MediumEditSheet(isOpen, refreshToken, mediumId));

        var detailsCard = new Card(
            content: new
                {
                    mediumValue.Id,
                    FilePath = mediumValue.FilePath,
                    FileType = mediumValue.FileType,
                    CustomerName = mediumValue.Customer != null ? $"{mediumValue.Customer.FirstName} {mediumValue.Customer.LastName}" : null,
                    LeadId = mediumValue.Lead?.Id,
                    VehicleInfo = mediumValue.Vehicle != null ? $"{mediumValue.Vehicle.Make} {mediumValue.Vehicle.Model} ({mediumValue.Vehicle.Year})" : null
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Media Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Messages", onClick: _ =>
                {
                    blades.Push(this, new MediumMessagesBlade(mediumId), "Messages");
                }, badge: messageCount.Value.ToString("N0"))
            ));

        return new Fragment()
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var medium = db.Media.FirstOrDefault(e => e.Id == mediumId)!;
        db.Media.Remove(medium);
        db.SaveChanges();
    }
}