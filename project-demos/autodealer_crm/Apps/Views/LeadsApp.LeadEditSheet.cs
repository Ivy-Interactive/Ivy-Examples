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
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput(QueryCustomers(factory), LookupCustomer(factory), placeholder: "Select Customer"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
            .Builder(e => e.SourceChannelId, e => e.ToAsyncSelectInput(QuerySourceChannels(factory), LookupSourceChannel(factory), placeholder: "Select Source Channel"))
            .Builder(e => e.LeadIntentId, e => e.ToAsyncSelectInput(QueryLeadIntents(factory), LookupLeadIntent(factory), placeholder: "Select Lead Intent"))
            .Builder(e => e.LeadStageId, e => e.ToAsyncSelectInput(QueryLeadStages(factory), LookupLeadStage(factory), placeholder: "Select Lead Stage"))
            .Builder(e => e.Priority, e => e.ToNumberInput())
            .Builder(e => e.Notes, e => e.ToTextAreaInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Lead");
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

    private static AsyncSelectQueryDelegate<int?> QuerySourceChannels(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.SourceChannels
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupSourceChannel(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var sourceChannel = await db.SourceChannels.FirstOrDefaultAsync(e => e.Id == id);
            if (sourceChannel == null) return null;
            return new Option<int?>(sourceChannel.DescriptionText, sourceChannel.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeadIntents(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.LeadIntents
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLeadIntent(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var leadIntent = await db.LeadIntents.FirstOrDefaultAsync(e => e.Id == id);
            if (leadIntent == null) return null;
            return new Option<int?>(leadIntent.DescriptionText, leadIntent.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeadStages(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.LeadStages
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLeadStage(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var leadStage = await db.LeadStages.FirstOrDefaultAsync(e => e.Id == id);
            if (leadStage == null) return null;
            return new Option<int?>(leadStage.DescriptionText, leadStage.Id);
        };
    }
}