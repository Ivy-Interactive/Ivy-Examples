using Ivy;

namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public class HistoryView : ViewBase
{
    record HistoryJobRow(string JobId, string Method, string State, string CreatedAt, string Duration, string? Error, string Actions);

    private class RecordBuilder(Func<HistoryJobRow, object?, object?> build) : IBuilder<HistoryJobRow>
    {
        public object? Build(object? value, HistoryJobRow record) => build(record, value);
    }

    public override object? Build()
    {
        var service = UseService<HangfireService>();
        var client = UseService<IClientProvider>();
        var filter = UseState("All");

        var query = UseQuery(
            key: ("job-history", filter.Value),
            fetcher: (ct) =>
            {
                var rows = new List<HistoryJobRow>();

                if (filter.Value is "All" or "Succeeded")
                {
                    foreach (var kvp in service.GetSucceededJobs(0, 50))
                    {
                        var dto = kvp.Value;
                        var method = dto.Job != null ? $"{dto.Job.Type.Name}.{dto.Job.Method.Name}" : "N/A";
                        var duration = dto.TotalDuration.HasValue
                            ? dto.TotalDuration.Value >= 1000 ? $"{dto.TotalDuration.Value / 1000.0:F1}s" : $"{dto.TotalDuration.Value}ms"
                            : "-";
                        rows.Add(new HistoryJobRow(kvp.Key, method, "Succeeded", dto.SucceededAt?.ToString("g") ?? "-", duration, null, ""));
                    }
                }

                if (filter.Value is "All" or "Failed")
                {
                    foreach (var kvp in service.GetFailedJobs(0, 50))
                    {
                        var dto = kvp.Value;
                        var method = dto.Job != null ? $"{dto.Job.Type.Name}.{dto.Job.Method.Name}" : "N/A";
                        rows.Add(new HistoryJobRow(kvp.Key, method, "Failed", dto.FailedAt?.ToString("g") ?? "-", "-", dto.ExceptionMessage, ""));
                    }
                }

                if (filter.Value is "All" or "Processing")
                {
                    foreach (var kvp in service.GetProcessingJobs(0, 50))
                    {
                        var dto = kvp.Value;
                        var method = dto.Job != null ? $"{dto.Job.Type.Name}.{dto.Job.Method.Name}" : "N/A";
                        rows.Add(new HistoryJobRow(kvp.Key, method, "Processing", dto.StartedAt?.ToString("g") ?? "-", "-", null, ""));
                    }
                }

                return Task.FromResult(rows.ToArray());
            },
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(5) }
        );

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var rows = query.Value ?? [];

        var filterOptions = new[] { "All", "Succeeded", "Failed", "Processing" };
        var filterInput = filter.ToSelectInput(filterOptions).WithField().Label("Filter by State");

        var table = new TableBuilder<HistoryJobRow>(rows)
            .Builder(x => x.State, _ => new RecordBuilder((row, _) =>
                new Badge(row.State).Variant(row.State switch
                {
                    "Succeeded" => BadgeVariant.Success,
                    "Failed" => BadgeVariant.Destructive,
                    "Processing" => BadgeVariant.Info,
                    _ => BadgeVariant.Secondary
                })))
            .Builder(x => x.Actions, _ => new RecordBuilder((row, _) =>
                row.State == "Failed"
                    ? new Button("Retry", () => { service.RetryFailedJob(row.JobId); client.Toast("Job requeued!"); query.Mutator.Revalidate(); }).Small()
                    : null))
            .Remove(x => x.Error)
            .Build();

        var failedRows = rows.Where(r => r.State == "Failed" && !string.IsNullOrEmpty(r.Error)).ToArray();

        var content = Layout.Vertical()
            | Text.H2("Execution History")
            | (Layout.Vertical().BottomMargin(2) | filterInput)
            | table;

        if (failedRows.Length > 0)
        {
            content = content
                | new Separator()
                | Text.H3("Error Details");
            foreach (var row in failedRows)
            {
                content = content | new Expandable($"Job {row.JobId} - {row.Method}", Text.P(row.Error ?? "No details"));
            }
        }

        return content;
    }
}
