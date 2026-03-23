using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Building, group: ["Apps"])]
public class StartupsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new StartupListBlade(), "Search");
        return blades;
    }
}