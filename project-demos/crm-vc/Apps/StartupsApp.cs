using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Building)]
public class StartupsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new StartupListBlade(), "Search");
    }
}