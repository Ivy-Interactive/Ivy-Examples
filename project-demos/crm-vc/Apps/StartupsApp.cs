using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Building, path: ["Apps"])]
public class StartupsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new StartupListBlade(), "Search");
    }
}