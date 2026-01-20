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
            .Builder(e => e.MessageChannelId, e => e.ToAsyncSelectInput<int>(QueryMessageChannels, LookupMessageChannel, placeholder: "Select Channel"))
            .Builder(e => e.MessageDirectionId, e => e.ToAsyncSelectInput<int>(QueryMessageDirections, LookupMessageDirection, placeholder: "Select Direction"))
            .Builder(e => e.MessageTypeId, e => e.ToAsyncSelectInput<int>(QueryMessageTypes, LookupMessageType, placeholder: "Select Type"))
            .Builder(e => e.MediaId, e => e.ToAsyncSelectInput<int?>(QueryMedia, LookupMedia, placeholder: "Select Media"))
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

    private static QueryResult<Option<int>[]> QueryMessageChannels(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryMessageChannels), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageChannels
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupMessageChannel(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupMessageChannel), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var channel = await db.MessageChannels.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (channel == null) return null;
                return new Option<int>(channel.DescriptionText, channel.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryMessageDirections(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryMessageDirections), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageDirections
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupMessageDirection(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupMessageDirection), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var direction = await db.MessageDirections.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (direction == null) return null;
                return new Option<int>(direction.DescriptionText, direction.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryMessageTypes(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryMessageTypes), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.MessageTypes
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupMessageType(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupMessageType), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var type = await db.MessageTypes.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (type == null) return null;
                return new Option<int>(type.DescriptionText, type.Id);
            });
    }

    private QueryResult<Option<int?>[]> QueryMedia(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string, int)>(
            key: (nameof(QueryMedia), query, customerId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Media
                        .Where(e => e.CustomerId == customerId && e.FilePath.Contains(query))
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