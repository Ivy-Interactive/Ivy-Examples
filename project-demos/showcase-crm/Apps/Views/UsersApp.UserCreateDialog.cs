namespace ShowcaseCrm.Apps.Views;

public class UserCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record UserCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";

        [Required]
        public string Email { get; init; } = "";
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var user = UseState(() => new UserCreateRequest());

        return user
            .ToForm()
            .Builder(e => e.Email, e => e.ToEmailInput())
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create User", submitTitle: "Create");

        async Task OnSubmit(UserCreateRequest request)
        {
            var userId = await CreateUserAsync(factory, request);
            refreshToken.Refresh(userId);
        }
    }

    private async Task<int> CreateUserAsync(ShowcaseCrmContextFactory factory, UserCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}