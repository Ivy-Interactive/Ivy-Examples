namespace AutodealerCrm.Apps.Views;

public class VehicleEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int vehicleId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var vehicle = UseState(() => factory.CreateDbContext().Vehicles.FirstOrDefault(e => e.Id == vehicleId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            vehicle.Value.UpdatedAt = DateTime.UtcNow;
            db.Vehicles.Update(vehicle.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [vehicle]);

        return vehicle
            .ToForm()
            .Builder(e => e.Make, e => e.ToTextInput())
            .Builder(e => e.Model, e => e.ToTextInput())
            .Builder(e => e.Year, e => e.ToNumberInput())
            .Builder(e => e.Vin, e => e.ToTextInput())
            .Builder(e => e.Price, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.VehicleStatusId, e => e.ToAsyncSelectInput(QueryVehicleStatuses(factory), LookupVehicleStatus(factory), placeholder: "Select Status"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt, e => e.ErpSyncId)
            .ToSheet(isOpen, "Edit Vehicle");
    }

    private static AsyncSelectQueryDelegate<int?> QueryVehicleStatuses(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.VehicleStatuses
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupVehicleStatus(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var status = await db.VehicleStatuses.FirstOrDefaultAsync(e => e.Id == id);
            if (status == null) return null;
            return new Option<int?>(status.DescriptionText, status.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryManagers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Users
                    .Where(e => e.Name.Contains(query))
                    .Select(e => new { e.Id, e.Name })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupManager(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id);
            if (manager == null) return null;
            return new Option<int?>(manager.Name, manager.Id);
        };
    }
}