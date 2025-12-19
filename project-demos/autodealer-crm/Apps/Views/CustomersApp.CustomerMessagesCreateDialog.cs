namespace AutodealerCrm.Apps.Views;

public class CustomerMessagesCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int customerId) : ViewBase
{
    private record MessageCreateRequest
    {
        [Required]
        public int MessageChannelId { get; init; }

        [Required]
        public int MessageDirectionId { get; init; }

        [Required]
        public int MessageTypeId { get; init; }

        public string? Content { get; init; }

        public int? MediaId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var message = UseState(() => new MessageCreateRequest());

        UseEffect(() =>
        {
            var messageId = CreateMessage(factory, message.Value);
            refreshToken.Refresh(messageId);
        }, [message]);

        return message
            .ToForm()
            .Builder(e => e.MessageChannelId, e => e.ToAsyncSelectInput(QueryMessageChannels(factory), LookupMessageChannel(factory), placeholder: "Select Channel"))
            .Builder(e => e.MessageDirectionId, e => e.ToAsyncSelectInput(QueryMessageDirections(factory), LookupMessageDirection(factory), placeholder: "Select Direction"))
            .Builder(e => e.MessageTypeId, e => e.ToAsyncSelectInput(QueryMessageTypes(factory), LookupMessageType(factory), placeholder: "Select Type"))
            .Builder(e => e.MediaId, e => e.ToAsyncSelectInput(QueryMedia(factory, customerId), LookupMedia(factory), placeholder: "Select Media"))
            .ToDialog(isOpen, title: "Create Message", submitTitle: "Create");
    }

    private int CreateMessage(AutodealerCrmContextFactory factory, MessageCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var message = new Message
        {
            CustomerId = customerId,
            MessageChannelId = request.MessageChannelId,
            MessageDirectionId = request.MessageDirectionId,
            MessageTypeId = request.MessageTypeId,
            Content = request.Content,
            MediaId = request.MediaId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        db.SaveChanges();

        return message.Id;
    }

    private static AsyncSelectQueryDelegate<int> QueryMessageChannels(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageChannels
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupMessageChannel(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var channel = await db.MessageChannels.FirstOrDefaultAsync(e => e.Id == id);
            if (channel == null) return null;
            return new Option<int>(channel.DescriptionText, channel.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int> QueryMessageDirections(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageDirections
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupMessageDirection(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var direction = await db.MessageDirections.FirstOrDefaultAsync(e => e.Id == id);
            if (direction == null) return null;
            return new Option<int>(direction.DescriptionText, direction.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int> QueryMessageTypes(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.MessageTypes
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupMessageType(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var type = await db.MessageTypes.FirstOrDefaultAsync(e => e.Id == id);
            if (type == null) return null;
            return new Option<int>(type.DescriptionText, type.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryMedia(AutodealerCrmContextFactory factory, int customerId)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Media
                    .Where(e => e.CustomerId == customerId && e.FilePath.Contains(query))
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