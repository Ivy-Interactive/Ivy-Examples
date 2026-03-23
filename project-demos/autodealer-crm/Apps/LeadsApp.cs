using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Target, group: ["Apps"])]
public class LeadsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new LeadListBlade(), "Search");
    }
}
