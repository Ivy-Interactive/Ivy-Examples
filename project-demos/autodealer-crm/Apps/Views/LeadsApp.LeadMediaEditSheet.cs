namespace AutodealerCrm.Apps.Views;

public class LeadMediaEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int mediaId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var media = UseState(() => factory.CreateDbContext().Media.FirstOrDefault(e => e.Id == mediaId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            media.Value.UpdatedAt = DateTime.UtcNow.ToString("O");
            db.Media.Update(media.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [media]);

        return media
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .Builder(e => e.UploadedAt, e => e.ToDateTimeInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput<int?>(QueryCustomers, LookupCustomer, placeholder: "Select Customer"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int?>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Builder(e => e.VehicleId, e => e.ToAsyncSelectInput<int?>(QueryVehicles, LookupVehicle, placeholder: "Select Vehicle"))
            .ToSheet(isOpen, "Edit Media");
    }

    private static QueryResult<Option<int?>[]> QueryCustomers(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryCustomers), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Customers
                        .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                        .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupCustomer(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupCustomer), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var customer = await db.Customers.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (customer == null) return null;
                return new Option<int?>(customer.FirstName + " " + customer.LastName, customer.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryLeads(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryLeads), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(e => e.Notes != null && e.Notes.Contains(query))
                        .Select(e => new { e.Id, e.Notes })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Notes, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupLead(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupLead), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (lead == null) return null;
                return new Option<int?>(lead.Notes, lead.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryVehicles(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryVehicles), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Vehicles
                        .Where(e => e.Make.Contains(query) || e.Model.Contains(query))
                        .Select(e => new { e.Id, Name = e.Make + " " + e.Model })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupVehicle(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupVehicle), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var vehicle = await db.Vehicles.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (vehicle == null) return null;
                return new Option<int?>(vehicle.Make + " " + vehicle.Model, vehicle.Id);
            });
    }
}