using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.PhoneCall, path: ["Apps"])]
public class CallRecordsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new CallRecordListBlade(), "Search");
    }
}
