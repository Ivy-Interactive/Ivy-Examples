namespace AutodealerCrm.Apps.Views;

public class VehicleListBlade : ViewBase
{
    private record VehicleListRecord(int Id, string Make, string Model, int Year, decimal Price);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int vehicleId)
            {
                blades.Pop(this, true);
                blades.Push(this, new VehicleDetailsBlade(vehicleId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var vehicle = (VehicleListRecord)e.Sender.Tag!;
            blades.Push(this, new VehicleDetailsBlade(vehicle.Id), $"{vehicle.Make} {vehicle.Model}");
        });

        ListItem CreateItem(VehicleListRecord record) =>
            new(title: $"{record.Make} {record.Model}", subtitle: $"{record.Year} - ${record.Price:N2}", onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Vehicle").ToTrigger((isOpen) => new VehicleCreateDialog(isOpen, refreshToken));

        return new FilteredListView<VehicleListRecord>(
            fetchRecords: (filter) => FetchVehicles(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<VehicleListRecord[]> FetchVehicles(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(v => v.Make.Contains(filter) || v.Model.Contains(filter));
        }

        return await linq
            .OrderByDescending(v => v.CreatedAt)
            .Take(50)
            .Select(v => new VehicleListRecord(v.Id, v.Make, v.Model, v.Year, v.Price))
            .ToArrayAsync();
    }
}