namespace Hangfire.Job.Dashboard.Apps.JobDashboard;

public static class SampleJobs
{
    public static void SuccessfulJob()
    {
        Thread.Sleep(500);
    }

    public static void SlowJob()
    {
        Thread.Sleep(3000);
    }

    public static void UnreliableJob()
    {
        Thread.Sleep(1000);
        if (Random.Shared.Next(2) == 0)
            throw new Exception("Random failure!");
    }

    public static void DataSyncJob()
    {
        Thread.Sleep(2000);
    }
}
