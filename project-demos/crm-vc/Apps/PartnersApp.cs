using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Users)]
public class PartnersApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new PartnerListBlade(), "Search");
    }
}