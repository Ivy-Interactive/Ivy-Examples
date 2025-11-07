using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.User, path: ["Apps"])]
public class FoundersApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new FounderListBlade(), "Search");
    }
}