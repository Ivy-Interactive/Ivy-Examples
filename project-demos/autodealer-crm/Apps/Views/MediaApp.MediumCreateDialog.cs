namespace AutodealerCrm.Apps.Views;

public class MediumCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record MediumCreateRequest
    {
        [Required]
        public string FilePath { get; init; } = "";

        [Required]
        public string FileType { get; init; } = "";

        [Required]
        public DateTime UploadedAt { get; init; } = DateTime.UtcNow;

        public int? VehicleId { get; init; } = null;

        public int? LeadId { get; init; } = null;

        public int? CustomerId { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var medium = UseState(() => new MediumCreateRequest());

        UseEffect(() =>
        {
            var mediumId = CreateMedium(factory, medium.Value);
            refreshToken.Refresh(mediumId);

        }, [medium]);

        return medium
            .ToForm()
            .Builder(e => e.VehicleId, e => e.ToAsyncSelectInput<int?>(QueryVehicles, LookupVehicle, placeholder: "Select Vehicle"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int?>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput<int?>(QueryCustomers, LookupCustomer, placeholder: "Select Customer"))
            .ToDialog(isOpen, title: "Create Media", submitTitle: "Create");
    }

    private int CreateMedium(AutodealerCrmContextFactory factory, MediumCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var medium = new Medium()
        {
            FilePath = request.FilePath,
            FileType = request.FileType,
            UploadedAt = request.UploadedAt.ToString("O"),
            VehicleId = request.VehicleId,
            LeadId = request.LeadId,
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        db.Media.Add(medium);
        db.SaveChanges();

        return medium.Id;
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
                        .Select(e => new { e.Id, Name = $"{e.Make} {e.Model} ({e.Year})" })
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
                return new Option<int?>($"{vehicle.Make} {vehicle.Model} ({vehicle.Year})", vehicle.Id);
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
                    .Select(e => new Option<int?>(e.Notes ?? "No Notes", e.Id))
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
                return new Option<int?>(lead.Notes ?? "No Notes", lead.Id);
            });
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
                        .Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" })
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
                return new Option<int?>($"{customer.FirstName} {customer.LastName}", customer.Id);
            });
    }
}