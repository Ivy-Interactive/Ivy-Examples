using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Job.Dashboard.Services;

public class HangfireService
{
    private readonly IMonitoringApi _monitoringApi;

    public HangfireService(IMonitoringApi monitoringApi)
    {
        _monitoringApi = monitoringApi;
    }

    public StatisticsDto GetStatistics()
    {
        return _monitoringApi.GetStatistics();
    }

    public void EnqueueJob(string type)
    {
        switch (type)
        {
            case "success":
                BackgroundJob.Enqueue(() => Console.WriteLine("Success job executed!"));
                break;
            case "slow":
                BackgroundJob.Enqueue(() => Thread.Sleep(5000));
                break;
            case "unreliable":
                BackgroundJob.Enqueue(() => UnreliableTask());
                break;
        }
    }

    public static void UnreliableTask()
    {
        if (Random.Shared.Next(2) == 0)
            throw new InvalidOperationException("Unreliable job failed!");
        Console.WriteLine("Unreliable job succeeded!");
    }
}
