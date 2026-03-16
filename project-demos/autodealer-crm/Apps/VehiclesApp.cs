using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Car, path: ["Apps"])]
public class VehiclesApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new VehicleListBlade(), "Search");
        return blades;
    }
}
