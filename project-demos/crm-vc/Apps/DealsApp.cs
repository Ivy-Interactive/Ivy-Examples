using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.DollarSign, path: ["Apps"])]
public class DealsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new DealListBlade(), "Search");
        return blades;
    }
}