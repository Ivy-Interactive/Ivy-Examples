using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Briefcase, group: ["Settings"])]
public class IndustriesApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new IndustryListBlade(), "Search");
        return blades;
    }
}