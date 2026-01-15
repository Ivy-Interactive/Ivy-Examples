namespace AutodealerCrm.Apps.Views;

public class CallRecordEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int callRecordId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var callRecord = UseState(() => factory.CreateDbContext().CallRecords.FirstOrDefault(e => e.Id == callRecordId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            callRecord.Value.UpdatedAt = DateTime.UtcNow;
            db.CallRecords.Update(callRecord.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [callRecord]);

        return callRecord
            .ToForm()
            .Builder(e => e.StartTime, e => e.ToDateTimeInput())
            .Builder(e => e.EndTime, e => e.ToDateTimeInput())
            .Builder(e => e.Duration, e => e.ToNumberInput())
            .Builder(e => e.RecordingUrl, e => e.ToUrlInput())
            .Builder(e => e.ScriptScore, e => e.ToTextInput())
            .Builder(e => e.Sentiment, e => e.ToTextInput())
            .Builder(e => e.CallDirectionId, e => e.ToAsyncSelectInput<int>(QueryCallDirections, LookupCallDirection, placeholder: "Select Call Direction"))
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput<int>(QueryCustomers, LookupCustomer, placeholder: "Select Customer"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int?>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput<int?>(QueryManagers, LookupManager, placeholder: "Select Manager"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Call Record");
    }

    private static QueryResult<Option<int>[]> QueryCallDirections(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryCallDirections), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.CallDirections
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupCallDirection(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupCallDirection), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var callDirection = await db.CallDirections.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (callDirection == null) return null;
                return new Option<int>(callDirection.DescriptionText, callDirection.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryCustomers(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryCustomers), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Customers
                        .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                        .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupCustomer(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupCustomer), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var customer = await db.Customers.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (customer == null) return null;
                return new Option<int>(customer.FirstName + " " + customer.LastName, customer.Id);
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

    private static QueryResult<Option<int?>[]> QueryManagers(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryManagers), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Users
                        .Where(e => e.Name.Contains(query))
                        .Select(e => new { e.Id, e.Name })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupManager(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupManager), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (manager == null) return null;
                return new Option<int?>(manager.Name, manager.Id);
            });
    }
}