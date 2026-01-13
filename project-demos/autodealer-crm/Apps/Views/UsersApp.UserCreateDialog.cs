namespace AutodealerCrm.Apps.Views;

public class UserCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record UserCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";

        [Required]
        public string Email { get; init; } = "";

        [Required]
        public int? UserRoleId { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var user = UseState(() => new UserCreateRequest());

        UseEffect(() =>
        {
            var userId = CreateUser(factory, user.Value);
            refreshToken.Refresh(userId);
        }, [user]);

        return user
            .ToForm()
            .Builder(e => e.UserRoleId, e => e.ToAsyncSelectInput<int?>(QueryUserRoles, LookupUserRole, placeholder: "Select Role"))
            .ToDialog(isOpen, title: "Create User", submitTitle: "Create");
    }

    private int CreateUser(AutodealerCrmContextFactory factory, UserCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var user = new User()
        {
            Name = request.Name,
            Email = request.Email,
            UserRoleId = request.UserRoleId!.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.SaveChanges();

        return user.Id;
    }

    private static QueryResult<Option<int?>[]> QueryUserRoles(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryUserRoles), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.UserRoles
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupUserRole(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupUserRole), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var role = await db.UserRoles.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (role == null) return null;
                return new Option<int?>(role.DescriptionText, role.Id);
            });
    }
}