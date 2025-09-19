using IvySample.DnsClient.Components;
using IvySample.DnsClient.Forms;

namespace IvySample.DnsClient.Apps;

[App(icon: Icons.Server, title:"Dns Lookup")]
public class DnsLookUpApp : ViewBase
{
    public override object? Build()
    {

        return Layout.Vertical().Gap(2)
             | Text.H1("Dns Client Sample")
             | new DnsLookupForm()
             | new DnsQueryResults();

    }
}
