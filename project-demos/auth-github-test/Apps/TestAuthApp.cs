namespace Auth.GitHub.Test.Apps;

[App(icon: Icons.Github, title: "GitHub Auth Test")]
public class TestAuthApp : ViewBase
{
    public override object? Build()
    {
        var auth = this.UseService<IAuthService>();
        var client = this.UseService<IClientProvider>();
        var userInfo = this.UseState<UserInfo?>();
        var loading = this.UseState<bool>(true);

        this.UseEffect(async () =>
        {
            try
            {
                var info = await auth.GetUserInfoAsync();
                userInfo.Set(info);
            }
            finally
            {
                loading.Set(false);
            }
        });

        if (loading.Value)
        {
            return Layout.Center()
                   | new Card(
                       Layout.Vertical().Gap(3)
                       | Icons.Github.ToIcon().Color(Colors.Primary)
                       | Text.H2("GitHub Authentication Test")
                     )
                     .Width(Size.Units(120).Max(600));
        }

        var isAuthenticated = userInfo.Value != null;
        return Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(4)
                   | (Layout.Horizontal().Align(Align.Center).Gap(2)
                      | Text.H2("GitHub Authentication Test"))
                   | (isAuthenticated
                        ? (Layout.Vertical().Gap(3)
                           | (Layout.Vertical().Gap(2).Align(Align.Center)
                              | new Avatar(userInfo.Value!.FullName ?? userInfo.Value.Id, userInfo.Value.AvatarUrl)
                                  .Height(60)
                                  .Width(60)
                              | Text.H3($"Welcome, {userInfo.Value.FullName ?? userInfo.Value.Id}!"))
                           | (new
                           {
                               UserID = userInfo.Value.Id,
                               Name = userInfo.Value.FullName ?? "N/A",
                               Email = userInfo.Value.Email ?? "N/A"
                           }).ToDetails())
                        : (Layout.Vertical().Gap(3)
                           | (Layout.Horizontal().Align(Align.Left).Gap(2)
                              | Text.Large("Not Authenticated"))
                           | Text.Block("Please click the login button in the navigation bar to authenticate with GitHub.")))
                 )
                 .Width(Size.Units(120).Max(600)));
    }
}

