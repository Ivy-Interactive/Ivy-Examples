using IvyInsights.Models;

namespace IvyInsights.Services;

public interface INuGetStatisticsProvider
{
    Task<PackageStatistics> GetPackageStatisticsAsync(string packageId, CancellationToken cancellationToken = default);
}

