using DnsClientExample.Components;
using DnsClientExample.Forms;

namespace DnsClientExample.Apps;

[App(icon: Icons.Server, title:"DNS Client")]
public class DnsLookUpApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Horizontal(
            new DnsLookupForm(),
            new Card(new DnsQueryResults()).Height(Size.Fit().Min(Size.Full()))
        );
    }
}
