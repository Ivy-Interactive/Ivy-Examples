using Ivy;
using Hangfire.Storage;

namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public class RecurringJobsView : ViewBase
{
    record RecurringJobRow(string Id, string Cron, string LastExecution, string NextExecution, string LastState);

    public override object? Build()
    {
        var service = UseService<HangfireService>();

        var jobName = UseState("");
        var cronExpression = UseState("*/5 * * * *");
        var jobType = UseState("success");

        var query = UseQuery(
            key: "recurring-jobs",
            fetcher: async (ct) => service.GetRecurringJobs(),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(5) }
        );

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var jobs = query.Value ?? new List<RecurringJobDto>();

        var rows = jobs.Select(j => new RecurringJobRow(
            j.Id,
            j.Cron,
            j.LastExecution?.ToString("g") ?? "Never",
            j.NextExecution?.ToString("g") ?? "N/A",
            j.LastJobState ?? "Unknown"
        )).ToArray();

        var jobTypeOptions = new[] { "success", "slow", "unreliable", "datasync" };

        var addButton = new Button("Add Recurring Job", () => { }).Primary().WithSheet(() =>
            Layout.Vertical()
                | jobName.ToTextInput().Placeholder("my-job").WithField().Label("Job Name")
                | cronExpression.ToTextInput().Placeholder("*/5 * * * *").WithField().Label("Cron Expression")
                | jobType.ToSelectInput(jobTypeOptions).WithField().Label("Job Type")
                | new Button("Submit", () =>
                {
                    var name = string.IsNullOrWhiteSpace(jobName.Value) ? $"job-{Guid.NewGuid():N}" : jobName.Value;
                    service.AddRecurringJob(name, cronExpression.Value, jobType.Value);
                    jobName.Set("");
                    cronExpression.Set("*/5 * * * *");
                    jobType.Set("success");
                    query.Mutator.Revalidate();
                }).Primary(),
            title: "Add Recurring Job"
        );

        var table = new TableBuilder<RecurringJobRow>(rows)
            .Header(r => r.Id, "Job Name")
            .Header(r => r.LastState, "Last State")
            .Builder(r => r.LastState, f => f.Func((string state) =>
                new Badge(state).Variant(state switch
                {
                    "Succeeded" => BadgeVariant.Success,
                    "Failed" => BadgeVariant.Destructive,
                    "Processing" => BadgeVariant.Info,
                    _ => BadgeVariant.Secondary
                }) as object))
            .Builder(r => r.Id, f => f.Func((string id) =>
                (Layout.Horizontal().Gap(2)
                    | Text.Block(id)
                    | new Button("Trigger", () =>
                    {
                        service.TriggerRecurringJob(id);
                        query.Mutator.Revalidate();
                    }).Small()
                    | new Button("Remove", () =>
                    {
                        service.RemoveRecurringJob(id);
                        query.Mutator.Revalidate();
                    }).Small().WithConfirm($"Remove recurring job '{id}'?", title: "Confirm", confirmLabel: "Remove", destructive: true)
                ) as object))
            .ColumnWidth(r => r.Id, Size.Fraction(0.35f))
            .Empty(Callout.Info("No recurring jobs found. Click 'Add Recurring Job' to create one."))
            .Build();

        return Layout.Vertical()
            | (Layout.Horizontal()
                | Text.H2("Recurring Jobs")
                | addButton)
            | table;
    }
}
