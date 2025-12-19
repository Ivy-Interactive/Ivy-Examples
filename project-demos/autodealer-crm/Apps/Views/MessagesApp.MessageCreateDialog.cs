namespace AutodealerCrm.Apps.Views;

public class MessageCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record MessageCreateRequest
    {
        [Required]
        public int CustomerId { get; init; }

        [Required]
        public int MessageChannelId { get; init; }

        [Required]
        public int MessageDirectionId { get; init; }

        [Required]
        public int MessageTypeId { get; init; }

        public string? Content { get; init; }

        public DateTime SentAt { get; init; } = DateTime.UtcNow;
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var messageRequest = UseState(() => new MessageCreateRequest());

        UseEffect(() =>
        {
            var messageId = CreateMessage(factory, messageRequest.Value);
            refreshToken.Refresh(messageId);
        }, [messageRequest]);

        return messageRequest
            .ToForm()
            .Builder(e => e.CustomerId, e => e.ToNumberInput())
            .Builder(e => e.MessageChannelId, e => e.ToNumberInput())
            .Builder(e => e.MessageDirectionId, e => e.ToNumberInput())
            .Builder(e => e.MessageTypeId, e => e.ToNumberInput())
            .Builder(e => e.Content, e => e.ToTextAreaInput())
            .Builder(e => e.SentAt, e => e.ToDateTimeInput())
            .ToDialog(isOpen, title: "Create Message", submitTitle: "Create");
    }

    private int CreateMessage(AutodealerCrmContextFactory factory, MessageCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var message = new Message()
        {
            CustomerId = request.CustomerId,
            MessageChannelId = request.MessageChannelId,
            MessageDirectionId = request.MessageDirectionId,
            MessageTypeId = request.MessageTypeId,
            Content = request.Content,
            SentAt = request.SentAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        db.SaveChanges();

        return message.Id;
    }
}