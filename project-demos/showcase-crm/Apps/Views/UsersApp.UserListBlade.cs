namespace ShowcaseCrm.Apps.Views;

public class UserListBlade : ViewBase
{
    private record UserListRecord(int Id, string Name, string Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");

        var usersQuery = UseUserListRecords(Context, filter.Value);

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int userId)
            {
                blades.Pop(this, true);
                usersQuery.Mutator.Revalidate();
                blades.Push(this, new UserDetailsBlade(userId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var user = (UserListRecord)e.Sender.Tag!;
            blades.Push(this, new UserDetailsBlade(user.Id), user.Name);
        });

        object CreateItem(UserListRecord listRecord) => new FuncView(context =>
        {
            var itemQuery = UseUserListRecord(context, listRecord);
            if (itemQuery.Loading || itemQuery.Value == null)
            {
                return new ListItem();
            }
            var record = itemQuery.Value;
            return new ListItem(title: record.Name, subtitle: record.Email, onClick: onItemClicked, tag: record);
        });

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create User").ToTrigger((isOpen) => new UserCreateDialog(isOpen, refreshToken));

        var items = (usersQuery.Value ?? []).Select(CreateItem);

        var header = Layout.Horizontal().Gap(1)
                     | filter.ToSearchInput().Placeholder("Search").Width(Size.Grow())
                     | createBtn;

        return new Fragment()
               | new BladeHeader(header)
               | (usersQuery.Value == null ? Text.Muted("Loading...") : new List(items));
    }

    private static QueryResult<UserListRecord[]> UseUserListRecords(IViewContext context, string filter)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseUserListRecords), filter),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var linq = db.Users.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    linq = linq.Where(e => e.Name.Contains(filter) || e.Email.Contains(filter));
                }

                return await linq
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new UserListRecord(e.Id, e.Name, e.Email))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(User[])],
            options: new QueryOptions()
            {
                KeepPrevious = true
            }
        );
    }

    private static QueryResult<UserListRecord?> UseUserListRecord(IViewContext context, UserListRecord record)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseUserListRecord), record.Id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Users
                    .Where(e => e.Id == record.Id)
                    .Select(e => new UserListRecord(e.Id, e.Name, e.Email))
                    .FirstOrDefaultAsync(ct);
            },
            options: new QueryOptions { RevalidateOnMount = false },
            initialValue: record,
            tags: [(typeof(User), record.Id)]
        );
    }
}