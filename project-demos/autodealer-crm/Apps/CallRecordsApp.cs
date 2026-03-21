using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.PhoneCall, group: ["Apps"])]
public class CallRecordsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new CallRecordListBlade(), "Search");
        return blades;
    }
}
