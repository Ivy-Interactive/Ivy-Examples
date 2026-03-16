using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Target, path: ["Apps"])]
public class LeadsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new LeadListBlade(), "Search");
        return blades;
    }
}
