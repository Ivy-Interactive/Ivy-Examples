using Ivy;

namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public class OverviewView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<HangfireService>();
        var client = UseService<IClientProvider>();
        var stats = UseQuery(
            key: "job-stats",
            fetcher: async (ct) => service.GetStatistics(),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(3) }
        );

        if (stats.Loading) return Skeleton.Card();
        if (stats.Error is { } error) return Callout.Error(error.Message);

        var s = stats.Value;

        return Layout.Vertical()
            | Text.H2("Job Dashboard Overview")
            | (Layout.Grid().Columns(3)
                | new MetricView("Succeeded", Icons.CircleCheck, ctx => ctx.UseQuery(
                    key: ("metric-succeeded", s.Succeeded),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Succeeded.ToString("N0"), null, null, null))))
                | new MetricView("Failed", Icons.CircleX, ctx => ctx.UseQuery(
                    key: ("metric-failed", s.Failed),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Failed.ToString("N0"), null, null, null))))
                | new MetricView("Processing", Icons.Loader, ctx => ctx.UseQuery(
                    key: ("metric-processing", s.Processing),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Processing.ToString("N0"), null, null, null))))
                | new MetricView("Scheduled", Icons.Calendar, ctx => ctx.UseQuery(
                    key: ("metric-scheduled", s.Scheduled),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Scheduled.ToString("N0"), null, null, null))))
                | new MetricView("Enqueued", Icons.Inbox, ctx => ctx.UseQuery(
                    key: ("metric-enqueued", s.Enqueued),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Enqueued.ToString("N0"), null, null, null))))
                | new MetricView("Recurring", Icons.RefreshCw, ctx => ctx.UseQuery(
                    key: ("metric-recurring", s.Recurring),
                    fetcher: () => Task.FromResult(new MetricRecord(s.Recurring.ToString("N0"), null, null, null)))))
            | new Separator()
            | Text.H3("Quick Actions")
            | (Layout.Horizontal()
                | new Button("Enqueue Success Job", () => { service.EnqueueJob("success"); client.Toast("Success job enqueued!"); }).Primary()
                | new Button("Enqueue Slow Job", () => { service.EnqueueJob("slow"); client.Toast("Slow job enqueued!"); })
                | new Button("Enqueue Unreliable Job", () => { service.EnqueueJob("unreliable"); client.Toast("Unreliable job enqueued!"); }));
    }
}
