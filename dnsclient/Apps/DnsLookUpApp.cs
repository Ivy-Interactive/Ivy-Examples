using DnsClientExample.Components;
using DnsClientExample.Forms;

namespace DnsClientExample.Apps;

[App(icon: Icons.Server, title:"DNS Lookup")]
public class DnsLookUpApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Vertical(
            Text.H1("🌐 DNS Lookup Tool"),
            Text.Muted("Query DNS records for any domain with detailed information"),
            new DnsLookupForm(),
            new DnsQueryResults()
        ).Width(Size.Units(200));
    }
}
