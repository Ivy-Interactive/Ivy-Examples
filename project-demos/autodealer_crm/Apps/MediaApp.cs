using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Image, path: ["Apps"])]
public class MediaApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new MediumListBlade(), "Search");
    }
}
