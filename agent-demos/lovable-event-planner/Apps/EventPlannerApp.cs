using Ivy;
using Lovable.Event.Planner.Apps.EventPlanner;

namespace Lovable.Event.Planner.Apps;

[App(icon: Icons.CalendarDays)]
public class EventPlannerApp : ViewBase
{
    public override object? Build()
    {
        var blades = UseBlades(() => new EventBrowseView(), "Events");
        return blades;
    }
}
