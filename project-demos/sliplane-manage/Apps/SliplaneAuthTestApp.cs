namespace SliplaneManage.Apps;

[App(icon: Icons.Rocket, title: "Sliplane Auth Test")]
public class SliplaneAuthTestApp : ViewBase
{
    public override object? Build()
    {
        var auth = this.UseService<IAuthService>();
        var userInfoState = this.UseState<UserInfo?>(default(UserInfo?));
        var loading = this.UseState<bool>(true);
        var error = this.UseState<string?>(default(string?));

        this.UseEffect(async () =>
        {
            try
            {
                var info = await auth.GetUserInfoAsync();
                userInfoState.Set(info);
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                loading.Set(false);
            }
        });

        if (loading.Value)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(3).Align(Align.Center)
                       | Icons.Rocket.ToIcon().Size(32)
                       | Text.H3("Checking Sliplane authentication...")
                       | Text.Muted("Waiting for auth state from SliplaneAuthProvider"));
        }

        if (error.Value is { Length: > 0 })
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(3)
                       | Text.H2("Error")
                       | Text.Block("Failed to get auth state from Sliplane.")
                       | Text.InlineCode(error.Value).Color(Colors.Red))
                     .Width(Size.Fraction(0.5f));
        }

        if (userInfoState.Value is null)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(3)
                       | Text.H2("Not authenticated")
                       | Text.Block("Use the auth menu in the Chrome shell to sign in with Sliplane.")
                       | Text.Muted("After successful login, reopen this app to verify the token."))
                     .Width(Size.Fraction(0.5f));
        }

        var session = auth.GetAuthSession();
        var hasToken = session.AuthToken?.AccessToken is { Length: > 0 };

        return Layout.Center()
               | new Card(Layout.Vertical().Gap(4)
                   | Text.H2("Sliplane authentication is working")
                   | Text.Block("The SliplaneAuthProvider returned a valid auth session.")
                   | (new
                      {
                          UserId = userInfoState.Value.Id,
                          Email = userInfoState.Value.Email,
                          Name = userInfoState.Value.FullName,
                          HasAccessToken = hasToken,
                      }).ToDetails()
                   | Text.Muted("Note: Sliplane does not expose a user-info endpoint, so values here may be placeholders from the provider.")).Width(Size.Fraction(0.6f));
    }
}

