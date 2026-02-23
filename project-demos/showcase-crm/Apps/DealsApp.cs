using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.DollarSign, path: ["Apps"])]
public class DealsApp : ViewBase
{
    public override object? Build()
    {
        return new DealsKanbanBlade();
    }
}
