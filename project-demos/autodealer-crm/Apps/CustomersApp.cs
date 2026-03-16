using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Users, path: ["Apps"])]
public class CustomersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new CustomerListBlade(), "Search");
        return blades;
    }
}
