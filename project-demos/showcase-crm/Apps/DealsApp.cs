using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.DollarSign, group: ["Apps"])]
public class DealsApp : ViewBase
{
    public override object? Build()
    {
        return new DealsKanbanBlade();
    }
}
