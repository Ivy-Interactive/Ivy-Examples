using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.User)]
public class FoundersApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new FounderListBlade(), "Search");
    }
}