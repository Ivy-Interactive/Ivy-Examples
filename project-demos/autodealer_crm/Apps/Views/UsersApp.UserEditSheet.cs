namespace AutodealerCrm.Apps.Views;

public class UserEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int userId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var user = UseState(() => factory.CreateDbContext().Users.FirstOrDefault(e => e.Id == userId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            user.Value.UpdatedAt = DateTime.UtcNow;
            db.Users.Update(user.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [user]);

        return user
            .ToForm()
            .Builder(e => e.Name, e => e.ToTextInput())
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.UserRoleId, e => e.ToAsyncSelectInput(QueryUserRoles(factory), LookupUserRole(factory), placeholder: "Select Role"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit User");
    }

    private static AsyncSelectQueryDelegate<int?> QueryUserRoles(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.UserRoles
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupUserRole(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var role = await db.UserRoles.FirstOrDefaultAsync(e => e.Id == id);
            if (role == null) return null;
            return new Option<int?>(role.DescriptionText, role.Id);
        };
    }
}