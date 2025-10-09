using DnsClientExample.Components;
using DnsClientExample.Forms;

namespace DnsClientExample.Apps;

[App(icon: Icons.Server, title:"DNS Lookup")]
public class DnsLookUpApp : ViewBase
{
    public override object? Build()
    {

        return Layout.Vertical().Gap(2)
             | Text.H1("DNS Client Sample")
             | new DnsLookupForm()
             | new DnsQueryResults();

    }
}
