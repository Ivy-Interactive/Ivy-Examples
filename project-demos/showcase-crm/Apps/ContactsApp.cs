using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Phone, group: ["Apps"])]
public class ContactsApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new ContactListBlade(), "Search");
        return blades;
    }
}
