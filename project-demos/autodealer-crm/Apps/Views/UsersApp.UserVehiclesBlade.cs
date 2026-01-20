namespace AutodealerCrm.Apps.Views;

public class UserVehiclesBlade(int? managerId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var vehicles = this.UseState<Vehicle[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            vehicles.Set(await db.Vehicles
                .Include(v => v.VehicleStatus)
                .Where(v => managerId == null || v.ManagerId == managerId)
                .ToArrayAsync());
        }, [ EffectTrigger.OnMount(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this vehicle?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Vehicle", AlertButtonSet.OkCancel);
            };
        }

        if (vehicles.Value == null) return null;

        var table = vehicles.Value.Select(v => new
            {
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                VIN = v.Vin,
                Price = v.Price,
                Status = v.VehicleStatus.DescriptionText,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(v.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new UserVehiclesEditSheet(isOpen, refreshToken, v.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Vehicle").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new UserVehiclesCreateDialog(isOpen, refreshToken, managerId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int vehicleId)
    {
        using var db = factory.CreateDbContext();
        db.Vehicles.Remove(db.Vehicles.Single(v => v.Id == vehicleId));
        db.SaveChanges();
    }
}