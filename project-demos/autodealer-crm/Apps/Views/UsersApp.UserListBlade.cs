namespace AutodealerCrm.Apps.Views;

public class UserListBlade : ViewBase
{
    private record UserListRecord(int Id, string Name, string Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int userId)
            {
                blades.Pop(this, true);
                blades.Push(this, new UserDetailsBlade(userId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var user = (UserListRecord)e.Sender.Tag!;
            blades.Push(this, new UserDetailsBlade(user.Id), user.Name);
        });

        ListItem CreateItem(UserListRecord record) =>
            new(title: record.Name, subtitle: record.Email, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create User").ToTrigger((isOpen) => new UserCreateDialog(isOpen, refreshToken));

        return new FilteredListView<UserListRecord>(
            fetchRecords: (filter) => FetchUsers(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<UserListRecord[]> FetchUsers(AutodealerCrmContextFactory factory, string filter)
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
            .Take(50)
            .Select(e => new UserListRecord(e.Id, e.Name, e.Email))
            .ToArrayAsync();
    }
}