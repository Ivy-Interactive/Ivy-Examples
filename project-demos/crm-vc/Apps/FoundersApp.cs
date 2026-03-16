using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.User, path: ["Apps"])]
public class FoundersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new FounderListBlade(), "Search");
        return blades;
    }
}