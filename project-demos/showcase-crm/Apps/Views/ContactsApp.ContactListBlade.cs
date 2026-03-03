namespace ShowcaseCrm.Apps.Views;

public class ContactListBlade : ViewBase
{
    private record ContactListRecord(int Id, string FirstName, string LastName, string? Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");

        var contactsQuery = UseContactListRecords(Context, filter.Value);

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int contactId)
            {
                blades.Pop(this, true);
                contactsQuery.Mutator.Revalidate();
                blades.Push(this, new ContactDetailsBlade(contactId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var contact = (ContactListRecord)e.Sender.Tag!;
            blades.Push(this, new ContactDetailsBlade(contact.Id), $"{contact.FirstName} {contact.LastName}");
        });

        object CreateItem(ContactListRecord listRecord) => new FuncView(context =>
        {
            var itemQuery = UseContactListRecord(context, listRecord);
            if (itemQuery.Loading || itemQuery.Value == null)
            {
                return new ListItem();
            }
            var record = itemQuery.Value;
            return new ListItem(
                title: $"{record.FirstName} {record.LastName}",
                subtitle: record.Email,
                onClick: onItemClicked,
                tag: record
            );
        });

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Contact").ToTrigger((isOpen) => new ContactCreateDialog(isOpen, refreshToken));

        var items = (contactsQuery.Value ?? []).Select(CreateItem);

        var header = Layout.Horizontal().Gap(1)
                     | filter.ToSearchInput().Placeholder("Search").Width(Size.Grow())
                     | createBtn;

        return new Fragment()
               | new BladeHeader(header)
               | (contactsQuery.Value == null ? Text.Muted("Loading...") : new List(items));
    }

    private static QueryResult<ContactListRecord[]> UseContactListRecords(IViewContext context, string filter)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactListRecords), filter),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var linq = db.Contacts.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    linq = linq.Where(e => e.FirstName.Contains(filter) || e.LastName.Contains(filter) || e.Email.Contains(filter));
                }

                return await linq
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new ContactListRecord(e.Id, e.FirstName, e.LastName, e.Email))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Contact[])],
            options: new QueryOptions()
            {
                KeepPrevious = true
            }
        );
    }

    private static QueryResult<ContactListRecord?> UseContactListRecord(IViewContext context, ContactListRecord record)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactListRecord), record.Id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Contacts
                    .Where(e => e.Id == record.Id)
                    .Select(e => new ContactListRecord(e.Id, e.FirstName, e.LastName, e.Email))
                    .FirstOrDefaultAsync(ct);
            },
            options: new QueryOptions { RevalidateOnMount = false },
            initialValue: record,
            tags: [(typeof(Contact), record.Id)]
        );
    }
}