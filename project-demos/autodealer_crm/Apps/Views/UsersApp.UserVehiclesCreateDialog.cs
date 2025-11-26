namespace AutodealerCrm.Apps.Views;

public class UserVehiclesCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? managerId) : ViewBase
{
    private record VehicleCreateRequest
    {
        [Required]
        public string Make { get; init; } = "";

        [Required]
        public string Model { get; init; } = "";

        [Required]
        public int Year { get; init; }

        [Required]
        public string Vin { get; init; } = "";

        [Required]
        public decimal Price { get; init; }

        [Required]
        public int VehicleStatusId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var vehicleState = UseState(() => new VehicleCreateRequest());

        UseEffect(() =>
        {
            var vehicleId = CreateVehicle(factory, vehicleState.Value, managerId);
            refreshToken.Refresh(vehicleId);
        }, [vehicleState]);

        return vehicleState
            .ToForm()
            .Builder(e => e.VehicleStatusId, e => e.ToAsyncSelectInput(QueryVehicleStatuses(factory), LookupVehicleStatus(factory), placeholder: "Select Vehicle Status"))
            .Builder(e => e.Price, e => e.ToMoneyInput().Currency("USD"))
            .ToDialog(isOpen, title: "Create Vehicle", submitTitle: "Create");
    }

    private int CreateVehicle(AutodealerCrmContextFactory factory, VehicleCreateRequest request, int? managerId)
    {
        using var db = factory.CreateDbContext();

        var vehicle = new Vehicle
        {
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Vin = request.Vin,
            Price = request.Price,
            VehicleStatusId = request.VehicleStatusId,
            ManagerId = managerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        db.SaveChanges();

        return vehicle.Id;
    }

    private static AsyncSelectQueryDelegate<int> QueryVehicleStatuses(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.VehicleStatuses
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupVehicleStatus(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var status = await db.VehicleStatuses.FirstOrDefaultAsync(e => e.Id == id);
            if (status == null) return null;
            return new Option<int>(status.DescriptionText, status.Id);
        };
    }
}