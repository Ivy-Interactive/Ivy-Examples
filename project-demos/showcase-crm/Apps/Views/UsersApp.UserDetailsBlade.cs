namespace ShowcaseCrm.Apps.Views;

public class UserDetailsBlade(int userId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var userQuery = UseQuery(
            key: (nameof(UserDetailsBlade), userId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Users.SingleOrDefaultAsync(e => e.Id == userId, ct);
            },
            tags: [(typeof(User), userId)]
        );

        if (userQuery.Loading) return Skeleton.Card();

        if (userQuery.Value == null)
        {
            return new Callout($"User '{userId}' not found. It may have been deleted.")
                .Variant(CalloutVariant.Warning);
        }

        var userValue = userQuery.Value;

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                blades.Pop(refresh: true);
                await DeleteAsync(factory);
                queryService.RevalidateByTag(typeof(User[]));
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .WithConfirm("Are you sure you want to delete this user?", "Delete User");

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new UserEditSheet(isOpen, refreshToken, userId));

        var detailsCard = new Card(
            content: new
                {
                    Id = userValue.Id,
                    Name = userValue.Name,
                    Email = userValue.Email,
                }
                .ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | deleteBtn
                    | editBtn
        ).Title("User Details").Width(Size.Units(100));

        return new Fragment()
               | new BladeHeader(Text.Literal(userValue.Name))
               | (Layout.Vertical() | detailsCard);
    }

    private async Task DeleteAsync(ShowcaseCrmContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(e => e.Id == userId);
        if (user != null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}