namespace AutodealerCrm.Apps.Views;

public class MediumMessagesCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? mediaId) : ViewBase
{
    private record MessageCreateRequest
    {
        [Required]
        public string Content { get; init; } = "";

        [Required]
        public int CustomerId { get; init; }

        public int? LeadId { get; init; }
        
        public int MessageChannelId { get; init; }

        public int MessageDirectionId { get; init; }

        public int MessageTypeId { get; init; }

        public int? MediaId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var message = UseState(() => new MessageCreateRequest());

        UseEffect(() =>
        {
            if (message.Value != null)
            {
                var messageId = CreateMessage(factory, message.Value);
                refreshToken.Refresh(messageId);
            }
        }, [message]);

        return message
            .ToForm()
            .Builder(e => e.Content, e => e.ToTextAreaInput())
            .Builder(e => e.CustomerId, e => e.ToNumberInput())
            .Builder(e => e.LeadId, e => e.ToNumberInput())
            .Builder(e => e.MessageChannelId, e => e.ToNumberInput())
            .Builder(e => e.MessageDirectionId, e => e.ToNumberInput())
            .Builder(e => e.MessageTypeId, e => e.ToNumberInput())
            .ToDialog(isOpen, title: "Create Message", submitTitle: "Create");
    }

    private int CreateMessage(AutodealerCrmContextFactory factory, MessageCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var message = new Message()
        {
            Content = request.Content,
            CustomerId = request.CustomerId,
            LeadId = request.LeadId,
            MessageChannelId = request.MessageChannelId,
            MessageDirectionId = request.MessageDirectionId,
            MessageTypeId = request.MessageTypeId,
            MediaId = request.MediaId,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        db.SaveChanges();

        return message.Id;
    }
}