using Vc.Apps.Views;

namespace Vc.Apps;

[App(icon: Icons.Briefcase, path: ["Settings"])]
public class IndustriesApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new IndustryListBlade(), "Search");
    }
}