using Ivy;
using Hangfire.Job.Dashboard.Apps.JobDashboard;

namespace Hangfire.Job.Dashboard.Apps;

[App(icon: Icons.Activity, group: new[] { "Monitoring" })]
public class JobDashboardApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Tabs(
            new Tab("Overview", new OverviewView()).Icon(Icons.ChartBar),
            new Tab("Recurring Jobs", new RecurringJobsView()).Icon(Icons.Clock),
            new Tab("Queues", new QueuesView()).Icon(Icons.Layers),
            new Tab("History", new HistoryView()).Icon(Icons.History));
    }
}
