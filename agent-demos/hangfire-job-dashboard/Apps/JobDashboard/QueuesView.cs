using Ivy;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public class QueuesView : ViewBase
{
    record QueueJobRow(string JobId, string Method, string State, string EnqueuedAt);

    public override object? Build()
    {
        var service = UseService<HangfireService>();

        var query = UseQuery(
            key: "queues",
            fetcher: async (ct) => service.GetQueues(),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(5) }
        );

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var queues = query.Value ?? (IList<QueueWithTopEnqueuedJobsDto>)[];

        if (queues.Count == 0)
        {
            return Layout.Vertical()
                | Text.H2("Queue Monitor")
                | Callout.Info("No queues found. Enqueue a job to see queue activity.");
        }

        var layout = Layout.Vertical()
            | Text.H2("Queue Monitor");

        foreach (var queue in queues)
        {
            var jobRows = queue.FirstJobs?
                .Select(kvp => new QueueJobRow(
                    kvp.Key,
                    kvp.Value?.Job?.ToString() ?? "N/A",
                    kvp.Value?.State ?? "Unknown",
                    kvp.Value?.EnqueuedAt?.ToString("g") ?? "N/A"
                )).ToArray() ?? [];

            var queueHeader = Layout.Horizontal().Gap(3)
                | Text.H3(queue.Name)
                | new Badge($"{queue.Length} jobs").Variant(queue.Length > 0 ? BadgeVariant.Info : BadgeVariant.Secondary);

            object? queueContent;
            if (jobRows.Length == 0)
            {
                queueContent = Text.Muted("No enqueued jobs");
            }
            else
            {
                queueContent = new TableBuilder<QueueJobRow>(jobRows)
                    .Header(r => r.JobId, "Job ID")
                    .Header(r => r.Method, "Method")
                    .Header(r => r.State, "State")
                    .Header(r => r.EnqueuedAt, "Enqueued At")
                    .Builder(r => r.State, f => f.Func((string state) =>
                        new Badge(state).Variant(state switch
                        {
                            "Enqueued" => BadgeVariant.Info,
                            "Processing" => BadgeVariant.Warning,
                            "Succeeded" => BadgeVariant.Success,
                            "Failed" => BadgeVariant.Destructive,
                            _ => BadgeVariant.Secondary
                        }) as object))
                    .Build();
            }

            var card = new Card(
                Layout.Vertical()
                    | queueHeader
                    | queueContent
            );

            layout |= card;
        }

        return layout;
    }
}
