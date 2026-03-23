using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Car, group: ["Apps"])]
public class VehiclesApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new VehicleListBlade(), "Search");
    }
}
