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

        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.CachedAt < CacheExpiration)
        {
            return cached.Data;
        }

        // 1. Registration API - source of truth for versions, published dates, and downloads
        var versions = await _apiClient.GetAllVersionsFromRegistrationAsync(packageId, cancellationToken);

        if (versions.Count == 0)
        {
            throw new Exception($"Package {packageId} not found or has no versions");
        }

        // Normalize: filter invalid dates and deduplicate
        var normalizedVersions = versions
            .Where(v => v.Published == null || v.Published.Value.Year >= 2000)
            .GroupBy(v => v.Version)
            .Select(g => g.First())
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .ToList();

        // 2. Get metadata from Search API
        var metadata = await _apiClient.GetPackageMetadataAsync(packageId, cancellationToken);

        // Calculate statistics
        var latest = normalizedVersions.First();
        var publishedDates = normalizedVersions
            .Where(v => v.Published.HasValue)
            .Select(v => v.Published!.Value)
            .ToList();

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
            FirstVersionPublished = publishedDates.Any()
                ? publishedDates.Min()
                : null,
            Versions = normalizedVersions
        };

        // Update cache
        _cache.AddOrUpdate(cacheKey, (statistics, DateTime.UtcNow), (key, old) => (statistics, DateTime.UtcNow));

        return statistics;
    }
}
