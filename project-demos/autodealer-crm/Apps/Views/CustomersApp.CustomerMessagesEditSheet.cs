namespace AutodealerCrm.Apps.Views;

public class CustomerMessagesEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int messageId) : ViewBase
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
            .Builder(e => e.MessageChannelId, e => e.ToAsyncSelectInput<int?>(QueryMessageChannels, LookupMessageChannel, placeholder: "Select Message Channel"))
            .Builder(e => e.MessageDirectionId, e => e.ToAsyncSelectInput<int?>(QueryMessageDirections, LookupMessageDirection, placeholder: "Select Message Direction"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Message");
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
}