using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Users, path: ["Apps"])]
public class PartnersApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new PartnerListBlade(), "Search");
        return blades;
    }
}