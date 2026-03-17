namespace ShowcaseCrm.Apps.Views;

public class ContactListBlade : ViewBase
{
    private record ContactListRecord(int Id, string FirstName, string LastName, string? Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");
        var searchFilter = UseState("");

        // Debounce search: update searchFilter 300ms after user stops typing (instant when clearing)
        UseEffect(async () =>
        {
            var cts = new CancellationTokenSource();
            if (string.IsNullOrWhiteSpace(filter.Value))
            {
                searchFilter.Value = filter.Value;
                return (IDisposable?)new DebounceDisposable(cts);
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, cts.Token);
                    searchFilter.Value = filter.Value;
                }
                catch (OperationCanceledException) { }
            });
            return new DebounceDisposable(cts);
        }, [filter]);

        var contactsQuery = UseContactListRecords(Context, searchFilter.Value);

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

        object CreateItem(ContactListRecord listRecord) => new ListItem(
            title: $"{listRecord.FirstName} {listRecord.LastName}",
            subtitle: listRecord.Email,
            onClick: onItemClicked,
            tag: listRecord
        );

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

    private sealed class DebounceDisposable(CancellationTokenSource cts) : IDisposable
    {
        public void Dispose() => cts.Cancel();
    }
}