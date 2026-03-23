using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.DollarSign, group: ["Apps"])]
public class DealsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new DealListBlade(), "Search");
        return blades;
    }
}