namespace AutodealerCrm.Apps.Views;

public class MessageEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int messageId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var message = UseState(() => factory.CreateDbContext().Messages.FirstOrDefault(e => e.Id == messageId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            message.Value.UpdatedAt = DateTime.UtcNow;
            db.Messages.Update(message.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [message]);

        return message
            .ToForm()
            .Builder(e => e.Content, e => e.ToTextAreaInput())
            .Builder(e => e.SentAt, e => e.ToDateTimeInput())
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput<int?>(QueryCustomers, LookupCustomer, placeholder: "Select Customer"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int?>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput<int?>(QueryManagers, LookupManager, placeholder: "Select Manager"))
            .Builder(e => e.MessageChannelId, e => e.ToAsyncSelectInput<int?>(QueryMessageChannels, LookupMessageChannel, placeholder: "Select Message Channel"))
            .Builder(e => e.MessageDirectionId, e => e.ToAsyncSelectInput<int?>(QueryMessageDirections, LookupMessageDirection, placeholder: "Select Message Direction"))
            .Builder(e => e.MessageTypeId, e => e.ToAsyncSelectInput<int?>(QueryMessageTypes, LookupMessageType, placeholder: "Select Message Type"))
            .Builder(e => e.MediaId, e => e.ToAsyncSelectInput<int?>(QueryMedia, LookupMedia, placeholder: "Select Media"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Message");
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

    private static QueryResult<Option<int?>[]> QueryLeads(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryLeads), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(e => e.Notes.Contains(query))
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

    private static QueryResult<Option<int?>[]> QueryMessageChannels(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryMessageChannels), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageChannels
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupMessageChannel(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupMessageChannel), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var channel = await db.MessageChannels.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (channel == null) return null;
                return new Option<int?>(channel.DescriptionText, channel.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryMessageDirections(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryMessageDirections), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageDirections
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupMessageDirection(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupMessageDirection), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var direction = await db.MessageDirections.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (direction == null) return null;
                return new Option<int?>(direction.DescriptionText, direction.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryMessageTypes(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryMessageTypes), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageTypes
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupMessageType(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupMessageType), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var type = await db.MessageTypes.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (type == null) return null;
                return new Option<int?>(type.DescriptionText, type.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryMedia(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryMedia), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Media
                        .Where(e => e.FilePath.Contains(query))
                        .Select(e => new { e.Id, e.FilePath })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.FilePath, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupMedia(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupMedia), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var media = await db.Media.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (media == null) return null;
                return new Option<int?>(media.FilePath, media.Id);
            });
    }
}