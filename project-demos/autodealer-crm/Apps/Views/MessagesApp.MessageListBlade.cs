namespace AutodealerCrm.Apps.Views;

public class MessageListBlade : ViewBase
{
    private record MessageListRecord(int Id, string Content, string CustomerName);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int messageId)
            {
                blades.Pop(this, true);
                blades.Push(this, new MessageDetailsBlade(messageId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var message = (MessageListRecord)e.Sender.Tag!;
            blades.Push(this, new MessageDetailsBlade(message.Id), message.Content);
        });

        ListItem CreateItem(MessageListRecord record) =>
            new(title: record.Content, subtitle: record.CustomerName, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Message").ToTrigger((isOpen) => new MessageCreateDialog(isOpen, refreshToken));

        return new FilteredListView<MessageListRecord>(
            fetchRecords: (filter) => FetchMessages(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<MessageListRecord[]> FetchMessages(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Messages
            .Include(m => m.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(m => m.Content.Contains(filter) || m.Customer.FirstName.Contains(filter) || m.Customer.LastName.Contains(filter));
        }

        return await linq
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new MessageListRecord(
                m.Id,
                m.Content ?? "No Content",
                $"{m.Customer.FirstName} {m.Customer.LastName}"
            ))
            .ToArrayAsync();
    }
}