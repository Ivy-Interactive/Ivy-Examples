using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.User, path: ["Apps"])]
public class UsersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new UserListBlade(), "Search");
        return blades;
    }
}
