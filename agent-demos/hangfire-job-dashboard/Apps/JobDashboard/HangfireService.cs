using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public class HangfireService(IBackgroundJobClient jobClient, IRecurringJobManager recurringJobManager)
{
    public StatisticsDto GetStatistics()
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.GetStatistics();
    }

    public JobList<SucceededJobDto> GetSucceededJobs(int from, int count)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.SucceededJobs(from, count);
    }

    public JobList<FailedJobDto> GetFailedJobs(int from, int count)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.FailedJobs(from, count);
    }

    public JobList<ProcessingJobDto> GetProcessingJobs(int from, int count)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.ProcessingJobs(from, count);
    }

    public JobList<ScheduledJobDto> GetScheduledJobs(int from, int count)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.ScheduledJobs(from, count);
    }

    public JobList<EnqueuedJobDto> GetEnqueuedJobs(string queue, int from, int count)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.EnqueuedJobs(queue, from, count);
    }

    public IList<QueueWithTopEnqueuedJobsDto> GetQueues()
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        return monitor.Queues();
    }

    public List<RecurringJobDto> GetRecurringJobs()
    {
        using var connection = JobStorage.Current.GetConnection();
        return StorageConnectionExtensions.GetRecurringJobs(connection);
    }

    public string EnqueueJob(string jobType)
    {
        return jobType switch
        {
            "success" => jobClient.Enqueue(() => SampleJobs.SuccessfulJob()),
            "slow" => jobClient.Enqueue(() => SampleJobs.SlowJob()),
            "unreliable" => jobClient.Enqueue(() => SampleJobs.UnreliableJob()),
            "datasync" => jobClient.Enqueue(() => SampleJobs.DataSyncJob()),
            _ => jobClient.Enqueue(() => SampleJobs.SuccessfulJob())
        };
    }

    public bool RetryFailedJob(string jobId)
    {
        return jobClient.Requeue(jobId);
    }

    public void AddRecurringJob(string id, string cronExpression, string jobType = "success")
    {
        switch (jobType)
        {
            case "slow":
                recurringJobManager.AddOrUpdate(id, () => SampleJobs.SlowJob(), cronExpression);
                break;
            case "unreliable":
                recurringJobManager.AddOrUpdate(id, () => SampleJobs.UnreliableJob(), cronExpression);
                break;
            case "datasync":
                recurringJobManager.AddOrUpdate(id, () => SampleJobs.DataSyncJob(), cronExpression);
                break;
            default:
                recurringJobManager.AddOrUpdate(id, () => SampleJobs.SuccessfulJob(), cronExpression);
                break;
        }
    }

    public void RemoveRecurringJob(string id)
    {
        recurringJobManager.RemoveIfExists(id);
    }

    public void TriggerRecurringJob(string id)
    {
        recurringJobManager.Trigger(id);
    }
}
