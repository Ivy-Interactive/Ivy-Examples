using System.Collections.Concurrent;
using IvyInsights.Models;

namespace IvyInsights.Services;

public class NuGetStatisticsProvider : INuGetStatisticsProvider
{
    private readonly NuGetApiClient _apiClient;
    private static readonly ConcurrentDictionary<string, (PackageStatistics Data, DateTime CachedAt)> _cache = new();
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public NuGetStatisticsProvider(NuGetApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PackageStatistics> GetPackageStatisticsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"stats_{packageId.ToLowerInvariant()}";

        if (CacheExpiration > TimeSpan.Zero && _cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.CachedAt < CacheExpiration)
            return cached.Data;

        var versions = await _apiClient.GetAllVersionsFromRegistrationAsync(packageId, cancellationToken);
        if (versions.Count == 0)
            throw new Exception($"Package {packageId} not found or has no versions");

        var normalizedVersions = versions
            .Where(v => v.Published == null || v.Published.Value.Year >= 2000)
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .ToList();

        var metadata = await _apiClient.GetPackageMetadataAsync(packageId, cancellationToken);
        
        var downloadsByVersion = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        if (metadata?.Versions != null)
        {
            foreach (var searchVersion in metadata.Versions)
            {
                var normalizedKey = NuGetApiClient.NormalizeVersion(searchVersion.Version);
                downloadsByVersion[normalizedKey] = (long?)searchVersion.Downloads;
            }
        }

        foreach (var version in normalizedVersions)
        {
            var normalizedKey = NuGetApiClient.NormalizeVersion(version.Version);
            if (downloadsByVersion.TryGetValue(normalizedKey, out var downloads))
            {
                version.Downloads = downloads;
            }
        }

        var unmatchedVersions = normalizedVersions
            .Where(v => !v.Downloads.HasValue)
            .Select(v => v.Version)
            .ToList();
        
        if (unmatchedVersions.Count > 0)
        {
            var additionalDownloads = await _apiClient.GetAdditionalVersionDownloadsAsync(packageId, unmatchedVersions, cancellationToken);
            foreach (var version in normalizedVersions.Where(v => !v.Downloads.HasValue))
            {
                var normalizedKey = NuGetApiClient.NormalizeVersion(version.Version);
                if (additionalDownloads.TryGetValue(normalizedKey, out var downloads))
                {
                    version.Downloads = downloads;
                }
            }
        }

        var latest = normalizedVersions.First();
        var publishedDates = normalizedVersions.Where(v => v.Published.HasValue).Select(v => v.Published!.Value).ToList();

        var statistics = new PackageStatistics
        {
            PackageId = packageId,
            Description = metadata?.Description,
            Authors = metadata?.Authors?.FirstOrDefault(),
            ProjectUrl = metadata?.ProjectUrl,
            TotalDownloads = metadata?.TotalDownloads,
            TotalVersions = normalizedVersions.Count,
            LatestVersion = latest.Version,
            LatestVersionPublished = latest.Published,
            FirstVersionPublished = publishedDates.Any() ? publishedDates.Min() : null,
            Versions = normalizedVersions
        };

        _cache.AddOrUpdate(cacheKey, (statistics, DateTime.UtcNow), (_, _) => (statistics, DateTime.UtcNow));
        return statistics;
    }
}

