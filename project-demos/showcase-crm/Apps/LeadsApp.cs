using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Target, path: ["Apps"])]
public class LeadsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new LeadListBlade(), "Search");
    }
}
