namespace ShowcaseCrm.Apps.Views;

public class UserEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int userId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();

        var userQuery = UseQuery(
            key: (typeof(User), userId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Users.FirstAsync(e => e.Id == userId, ct);
            },
            tags: [(typeof(User), userId)]
        );

        if (userQuery.Loading || userQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit User");

        return userQuery.Value
            .ToForm()
            .Builder(e => e.Name, e => e.ToTextInput())
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit User");

        async Task OnSubmit(User? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Users.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(User), userId));
        }
    }
}