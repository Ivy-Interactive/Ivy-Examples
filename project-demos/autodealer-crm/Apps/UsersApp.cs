using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.User, path: ["Settings"])]
public class UsersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new UserListBlade(), "Search");
        return blades;
    }
}
