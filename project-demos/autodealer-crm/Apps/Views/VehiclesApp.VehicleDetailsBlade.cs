namespace AutodealerCrm.Apps.Views;

public class VehicleDetailsBlade(int vehicleId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<AutodealerCrmContextFactory>();
        var blades = this.UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();
        var vehicle = this.UseState<Vehicle?>();
        var mediaCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            vehicle.Set(await db.Vehicles
                .Include(e => e.VehicleStatus)
                .Include(e => e.Manager)
                .SingleOrDefaultAsync(e => e.Id == vehicleId));
            mediaCount.Set(await db.Media.CountAsync(e => e.VehicleId == vehicleId));
        }, [EffectTrigger.OnMount(), refreshToken]);

        if (vehicle.Value == null) return null;

        var vehicleValue = vehicle.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this vehicle?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Vehicle", AlertButtonSet.OkCancel);
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
            .ToTrigger((isOpen) => new VehicleEditSheet(isOpen, refreshToken, vehicleId));

        var detailsCard = new Card(
            content: new
                {
                    vehicleValue.Id,
                    vehicleValue.Make,
                    vehicleValue.Model,
                    vehicleValue.Year,
                    vehicleValue.Vin,
                    vehicleValue.Price,
                    Status = vehicleValue.VehicleStatus.DescriptionText,
                    ManagerName = vehicleValue.Manager?.Name ?? "N/A"
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Vehicle Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Media", onClick: _ =>
                {
                    blades.Push(this, new VehicleMediaBlade(vehicleId), "Media");
                }, badge: mediaCount.Value.ToString("N0"))
            ));

        return new Fragment()
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var vehicle = db.Vehicles.FirstOrDefault(e => e.Id == vehicleId)!;
        db.Vehicles.Remove(vehicle);
        db.SaveChanges();
    }
}