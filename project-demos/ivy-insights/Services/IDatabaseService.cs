using IvyInsights.Models;

namespace IvyInsights.Services;

public interface IDatabaseService
{
    Task<List<DailyDownloadStats>> GetDailyDownloadStatsAsync(int days = 30, CancellationToken cancellationToken = default);
}

