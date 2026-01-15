namespace AutodealerCrm.Apps.Views;

public class LeadEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var lead = UseState(() => factory.CreateDbContext().Leads.FirstOrDefault(e => e.Id == leadId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            lead.Value.UpdatedAt = DateTime.UtcNow;
            db.Leads.Update(lead.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [lead]);

        return lead
            .ToForm()
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput<int?>(QueryCustomers, LookupCustomer, placeholder: "Select Customer"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput<int?>(QueryManagers, LookupManager, placeholder: "Select Manager"))
            .Builder(e => e.SourceChannelId, e => e.ToAsyncSelectInput<int?>(QuerySourceChannels, LookupSourceChannel, placeholder: "Select Source Channel"))
            .Builder(e => e.LeadIntentId, e => e.ToAsyncSelectInput<int?>(QueryLeadIntents, LookupLeadIntent, placeholder: "Select Lead Intent"))
            .Builder(e => e.LeadStageId, e => e.ToAsyncSelectInput<int?>(QueryLeadStages, LookupLeadStage, placeholder: "Select Lead Stage"))
            .Builder(e => e.Priority, e => e.ToNumberInput())
            .Builder(e => e.Notes, e => e.ToTextAreaInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Lead");
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

    private static QueryResult<Option<int?>[]> QuerySourceChannels(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QuerySourceChannels), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.SourceChannels
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupSourceChannel(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupSourceChannel), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var sourceChannel = await db.SourceChannels.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (sourceChannel == null) return null;
                return new Option<int?>(sourceChannel.DescriptionText, sourceChannel.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryLeadIntents(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryLeadIntents), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadIntents
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupLeadIntent(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupLeadIntent), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var leadIntent = await db.LeadIntents.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (leadIntent == null) return null;
                return new Option<int?>(leadIntent.DescriptionText, leadIntent.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryLeadStages(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryLeadStages), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadStages
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupLeadStage(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupLeadStage), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var leadStage = await db.LeadStages.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (leadStage == null) return null;
                return new Option<int?>(leadStage.DescriptionText, leadStage.Id);
            });
    }
}