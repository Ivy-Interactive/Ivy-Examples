namespace AutodealerCrm.Apps.Views;

public class MediumEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int mediumId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var medium = UseState(() => factory.CreateDbContext().Media.FirstOrDefault(e => e.Id == mediumId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            medium.Value.UpdatedAt = DateTime.UtcNow.ToString("O");
            db.Media.Update(medium.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [medium]);

        return medium
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .Builder(e => e.UploadedAt, e => e.ToDateTimeInput())
            .Builder(e => e.VehicleId, e => e.ToAsyncSelectInput(QueryVehicles(factory), LookupVehicle(factory), placeholder: "Select Vehicle"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(QueryLeads(factory), LookupLead(factory), placeholder: "Select Lead"))
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput(QueryCustomers(factory), LookupCustomer(factory), placeholder: "Select Customer"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Media");
    }

    private static AsyncSelectQueryDelegate<int?> QueryVehicles(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Vehicles
                    .Where(e => e.Make.Contains(query) || e.Model.Contains(query))
                    .Select(e => new { e.Id, Name = $"{e.Make} {e.Model} ({e.Year})" })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupVehicle(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(e => e.Id == id);
            if (vehicle == null) return null;
            return new Option<int?>($"{vehicle.Make} {vehicle.Model} ({vehicle.Year})", vehicle.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeads(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Leads
                    .Where(e => e.Notes != null && e.Notes.Contains(query))
                    .Select(e => new { e.Id, e.Notes })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Notes ?? "No Notes", e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLead(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id);
            if (lead == null) return null;
            return new Option<int?>(lead.Notes ?? "No Notes", lead.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryCustomers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Customers
                    .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                    .Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupCustomer(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var customer = await db.Customers.FirstOrDefaultAsync(e => e.Id == id);
            if (customer == null) return null;
            return new Option<int?>($"{customer.FirstName} {customer.LastName}", customer.Id);
        };
    }
}