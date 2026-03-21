using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.MessageCircle, group: ["Apps"])]
public class MessagesApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new MessageListBlade(), "Search");
        return blades;
    }
}
