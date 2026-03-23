using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.User, group: ["Apps"])]
public class UsersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new UserListBlade(), "Search");
        return blades;
    }
}
