namespace AutodealerCrm.Apps.Views;

public class UserMessagesEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int messageId) : ViewBase
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
            .Builder(e => e.MessageChannelId, e => e.ToAsyncSelectInput(QueryMessageChannels(factory), LookupMessageChannel(factory), placeholder: "Select Message Channel"))
            .Builder(e => e.MessageDirectionId, e => e.ToAsyncSelectInput(QueryMessageDirections(factory), LookupMessageDirection(factory), placeholder: "Select Message Direction"))
            .Builder(e => e.MessageTypeId, e => e.ToAsyncSelectInput(QueryMessageTypes(factory), LookupMessageType(factory), placeholder: "Select Message Type"))
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput(QueryCustomers(factory), LookupCustomer(factory), placeholder: "Select Customer"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
            .Builder(e => e.MediaId, e => e.ToAsyncSelectInput(QueryMedia(factory), LookupMedia(factory), placeholder: "Select Media"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Message");
    }

    private static AsyncSelectQueryDelegate<int?> QueryMessageChannels(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageChannels
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupMessageChannel(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var channel = await db.MessageChannels.FirstOrDefaultAsync(e => e.Id == id);
            if (channel == null) return null;
            return new Option<int?>(channel.DescriptionText, channel.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryMessageDirections(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageDirections
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupMessageDirection(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var direction = await db.MessageDirections.FirstOrDefaultAsync(e => e.Id == id);
            if (direction == null) return null;
            return new Option<int?>(direction.DescriptionText, direction.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryMessageTypes(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageTypes
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupMessageType(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var type = await db.MessageTypes.FirstOrDefaultAsync(e => e.Id == id);
            if (type == null) return null;
            return new Option<int?>(type.DescriptionText, type.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryCustomers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Customers
                    .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                    .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
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
            return new Option<int?>(customer.FirstName + " " + customer.LastName, customer.Id);
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

    private static AsyncSelectQueryDelegate<int?> QueryMedia(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Media
                    .Where(e => e.FilePath.Contains(query))
                    .Select(e => new { e.Id, e.FilePath })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.FilePath, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupMedia(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var media = await db.Media.FirstOrDefaultAsync(e => e.Id == id);
            if (media == null) return null;
            return new Option<int?>(media.FilePath, media.Id);
        };
    }
}