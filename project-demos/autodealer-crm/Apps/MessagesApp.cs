using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.MessageCircle, path: ["Apps"])]
public class MessagesApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new MessageListBlade(), "Search");
    }
}
