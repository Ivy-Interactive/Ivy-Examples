using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.User, path: ["Apps"])]
public class UsersApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new UserListBlade(), "Search");
    }
}
