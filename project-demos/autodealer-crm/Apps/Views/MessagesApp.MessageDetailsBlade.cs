namespace AutodealerCrm.Apps.Views;

public class MessageDetailsBlade(int messageId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();
        var message = UseState<Message?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            message.Set(await db.Messages
                .Include(e => e.Customer)
                .Include(e => e.Lead)
                .Include(e => e.Manager)
                .Include(e => e.Media)
                .Include(e => e.MessageChannel)
                .Include(e => e.MessageDirection)
                .Include(e => e.MessageType)
                .SingleOrDefaultAsync(e => e.Id == messageId));
        }, [EffectTrigger.OnMount(), refreshToken]);

        if (message.Value == null) return null;

        var messageValue = message.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this message?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Message", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(onDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger(isOpen => new MessageEditSheet(isOpen, refreshToken, messageId));

        var detailsCard = new Card(
            content: new
                {
                    Id = messageValue.Id,
                    Customer = $"{messageValue.Customer.FirstName} {messageValue.Customer.LastName}",
                    Lead = messageValue.Lead?.Id,
                    Manager = messageValue.Manager?.Name,
                    Channel = messageValue.MessageChannel.DescriptionText,
                    Direction = messageValue.MessageDirection.DescriptionText,
                    Type = messageValue.MessageType.DescriptionText,
                    Content = messageValue.Content,
                    Media = messageValue.Media?.FilePath,
                    SentAt = messageValue.SentAt
                }
                .ToDetails()
                .MultiLine(e => e.Content)
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Message Details").Width(Size.Units(100));

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var message = db.Messages.FirstOrDefault(e => e.Id == messageId)!;
        db.Messages.Remove(message);
        db.SaveChanges();
    }
}