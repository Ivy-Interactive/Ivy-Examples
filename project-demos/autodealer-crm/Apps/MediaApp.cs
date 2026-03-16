using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.Image, path: ["Apps"])]
public class MediaApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new MediumListBlade(), "Search");
        return blades;
    }
}
