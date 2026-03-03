using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Phone, path: ["Apps"])]
public class ContactsApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new ContactListBlade(), "Search");
    }
}
