using AutodealerCrm.Apps.Views;

namespace AutodealerCrm.Apps;

[App(icon: Icons.ListCheck, group: ["Apps"])]
public class TasksApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new TaskListBlade(), "Search");
        return blades;
    }
}
