using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Target, group: ["Apps"])]
public class LeadsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new LeadListBlade(), "Search");
        return blades;
    }
}
