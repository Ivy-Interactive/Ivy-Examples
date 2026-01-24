using IvyInsights.Models;
using IvyInsights.Services;
using Ivy.Helpers;

namespace IvyInsights.Apps;

internal class VersionChartDataItem
{
    public string Version { get; set; } = string.Empty;
    public double Downloads { get; set; }
    public bool HasDownloads { get; set; }
    public bool IsTopVersion { get; set; }
}

[App(icon: Icons.ChartBar, title: "NuGet Statistics")]
public class NuGetStatsApp : ViewBase
{
    private const string PackageId = "Ivy";

    private static bool IsPreRelease(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;
        
        // Pre-releases in NuGet have a hyphen after the version number
        // Examples: "1.0.0-alpha", "1.0.0-beta.1", "1.0.0-rc.1", "1.0.0-preview"
        var parts = version.Split('-');
        return parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]);
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var nugetProvider = UseService<INuGetStatisticsProvider>();
        
        // ============================================================================
        // UseQuery Hook - Automatic Data Fetching, Caching, and State Management
        // ============================================================================
        // Benefits of UseQuery over manual state management:
        // 1. Automatic caching: Data is cached and shared across components/users
        // 2. Stale-while-revalidate: Shows cached data immediately while fetching fresh data
        // 3. Built-in loading/error states: No need for manual loading/error state management
        // 4. Background revalidation: Automatically keeps data fresh
        // 5. Optimistic updates: Can update UI immediately, then sync with server
        // 6. Request deduplication: Multiple components requesting same data = single request
        // 7. Automatic retry: Built-in error handling and retry logic
        // ============================================================================
        var statsQuery = this.UseQuery(
            key: $"nuget-stats/{PackageId}", // Unique cache key - changing this invalidates cache
            fetcher: async (CancellationToken ct) =>
            {
                // NuGet API calls can take 10-30 seconds, so we use the provided cancellation token
                // UseQuery automatically cancels this if component unmounts or key changes
                var statistics = await nugetProvider.GetPackageStatisticsAsync(PackageId, ct);
                
                // Show success toast (safe to call, doesn't use request lifecycle token)
                try
                {
                    client.Toast($"Successfully loaded statistics for {PackageId}!");
                }
                catch
                {
                    // Ignore toast errors (e.g., if client is disposed)
                }
                
                return statistics;
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,              // Cache shared across all users on server
                Expiration = TimeSpan.FromMinutes(15),  // Cache TTL: 15 minutes (matches provider cache)
                KeepPrevious = true,                     // Show old data while fetching new (smooth UX)
                RevalidateOnMount = true                 // Fetch on component mount (first load)
            },
            tags: ["nuget", "statistics"]); // Tags allow bulk invalidation: queryService.InvalidateByTag("nuget")
        
        // Animated values for count-up effect
        var animatedDownloads = this.UseState(0L);
        var animatedVersions = this.UseState(0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        // Version chart filters
        var versionChartFromDate = this.UseState<DateTime?>(() => null);
        var versionChartToDate = this.UseState<DateTime?>(() => null);
        var versionChartShowPreReleases = this.UseState(true);
        var versionChartCount = this.UseState(7);

        // UseQuery for filtered version chart data - automatically updates when filters change
        // Key includes filter values, so changing filters will automatically trigger re-fetch
        var filteredVersionChartQuery = this.UseQuery(
            key: $"version-chart-filtered/{PackageId}/{versionChartFromDate.Value?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartToDate.Value?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartShowPreReleases.Value}/{versionChartCount.Value}",
            fetcher: async (CancellationToken ct) =>
            {
                // Wait for main stats query to be ready
                await Task.Yield(); // Ensure async
                if (statsQuery.Value == null)
                    return new List<VersionChartDataItem>();

                var s = statsQuery.Value;
                var count = Math.Clamp(versionChartCount.Value, 2, 20);
                var filteredVersions = s.Versions.AsEnumerable();
                
                // Filter by date range
                if (versionChartFromDate.Value.HasValue)
                {
                    var fromDate = versionChartFromDate.Value.Value.Date;
                    filteredVersions = filteredVersions.Where(v => 
                        v.Published.HasValue && v.Published.Value.Date >= fromDate);
                }
                if (versionChartToDate.Value.HasValue)
                {
                    var toDate = versionChartToDate.Value.Value.Date.AddDays(1);
                    filteredVersions = filteredVersions.Where(v => 
                        v.Published.HasValue && v.Published.Value.Date < toDate);
                }
                
                // Filter pre-releases
                if (!versionChartShowPreReleases.Value)
                {
                    filteredVersions = filteredVersions.Where(v => !IsPreRelease(v.Version));
                }
                
                // Filter versions with downloads (same as Top Popular Versions)
                // Only show versions that have download data
                filteredVersions = filteredVersions.Where(v => v.Downloads.HasValue && v.Downloads.Value > 0);
                
                // Order by downloads (most downloaded first) - same as Top Popular Versions
                var versionChartData = filteredVersions
                    .OrderByDescending(v => v.Downloads)
                    .Take(count)
                    .Select(v => new VersionChartDataItem
                    { 
                        Version = v.Version, 
                        Downloads = (double)v.Downloads!.Value,
                        HasDownloads = true, // All versions here have downloads
                        IsTopVersion = false // Will be set later if needed
                    })
                    .ToList();

                return versionChartData;
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server, // Server scope, but with zero expiration for immediate recalculation
                Expiration = TimeSpan.Zero, // No caching for filtered data - always recalculate
                KeepPrevious = false, // Don't keep previous data when filters change
                RevalidateOnMount = false // Don't revalidate on mount, only when key changes
            });

        // Animate numbers when data is loaded
        if (statsQuery.Value != null && !hasAnimated.Value)
        {
            var animStats = statsQuery.Value;
            var animTotalDownloads = animStats.TotalDownloads ?? 0;
            var animTotalVersions = animStats.TotalVersions;
            

            var scheduler = new JobScheduler(maxParallelJobs: 3);
            var steps = 60;
            var delayMs = 15;

            scheduler.CreateJob("Animate Metrics")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentProgress = i / (double)steps;
                        animatedDownloads.Set((long)(animTotalDownloads * currentProgress));
                        animatedVersions.Set((int)(animTotalVersions * currentProgress));
                        refresh.Refresh();
                        progress.Report(currentProgress);
                        await Task.Delay(delayMs, token);
                    }
                    animatedDownloads.Set(animTotalDownloads);
                    animatedVersions.Set(animTotalVersions);
                    refresh.Refresh();
                })
                .Build();

            _ = Task.Run(async () => await scheduler.RunAsync());
            hasAnimated.Set(true);
        }

        // Handle error state using UseQuery's built-in error handling
        if (statsQuery.Error is { } error)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Error")
                        | Text.Block(error.Message)
                        | new Button("Retry", onClick: _ => statsQuery.Mutator.Revalidate())
                            .Icon(Icons.RefreshCcw)
                ).Width(Size.Fraction(0.5f));
        }

        // Handle loading state - UseQuery shows loading during initial fetch
        // KeepPrevious option shows previous data during revalidation
        if (statsQuery.Loading && statsQuery.Value == null)
        {
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | Text.H1("NuGet Statistics")
                | Text.Muted($"Loading statistics for {PackageId}...")
                | (Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(0.9f))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | (Layout.Grid().Columns(3).Gap(3).Width(Size.Fraction(0.9f))
                    | new Skeleton().Height(Size.Units(200))
                    | new Skeleton().Height(Size.Units(200))
                    | new Skeleton().Height(Size.Units(200)))
                | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                    | new Skeleton().Width(Size.Fraction(0.6f)).Height(Size.Units(200))
                    | (Layout.Vertical().Width(Size.Full())
                        | new Skeleton().Height(Size.Units(200))
                        | new Skeleton().Height(Size.Units(200))));
        }

        var s = statsQuery.Value!;

        // Calculate metrics
        var totalDownloads = s.TotalDownloads ?? 0;
        var versionsWithDownloads = s.Versions.Count(v => v.Downloads.HasValue && v.Downloads.Value > 0);
        
        // Most popular version: use version with downloads if available, otherwise use latest version
        var mostDownloadedVersion = s.Versions
            .Where(v => v.Downloads.HasValue && v.Downloads.Value > 0)
            .OrderByDescending(v => v.Downloads)
            .FirstOrDefault();
        
        // Fallback to latest version if no download data available
        if (mostDownloadedVersion == null)
        {
            mostDownloadedVersion = s.Versions
                .OrderByDescending(v => v.Published ?? DateTime.MinValue)
                .FirstOrDefault();
        }


        // Calculate growth metrics using calendar months for consistency
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart;
        
        var versionsLastMonth = s.Versions
            .Count(v => v.Published.HasValue && 
                       v.Published.Value >= lastMonthStart && 
                       v.Published.Value < lastMonthEnd);
        var versionsThisMonth = s.Versions
            .Count(v => v.Published.HasValue && 
                       v.Published.Value >= thisMonthStart && 
                       v.Published.Value < now);
        
        // Downloads for versions published in the last calendar month
        var downloadsLastMonth = s.Versions
            .Where(v => v.Published.HasValue && 
                       v.Published.Value >= lastMonthStart && 
                       v.Published.Value < lastMonthEnd &&
                       v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);
        
        // Downloads this month (versions published this calendar month)
        var downloadsThisMonth = s.Versions
            .Where(v => v.Published.HasValue && 
                       v.Published.Value >= thisMonthStart && 
                       v.Published.Value < now &&
                       v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);

        // Calculate average monthly downloads over last 6 months (excluding current month)
        var monthlyDownloads = new List<long>();
        for (int i = 1; i <= 6; i++)
        {
            var monthStart = thisMonthStart.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthDownloads = s.Versions
                .Where(v => v.Published.HasValue && 
                           v.Published.Value >= monthStart && 
                           v.Published.Value < monthEnd &&
                           v.Downloads.HasValue)
                .Sum(v => v.Downloads!.Value);
            monthlyDownloads.Add(monthDownloads);
        }
        
        var avgMonthlyDownloads = monthlyDownloads.Count > 0 
            ? monthlyDownloads.Average() 
            : 0.0;

        // Prepare monthly downloads data for chart - all months with versions
        var monthlyChartData = s.Versions
            .Where(v => v.Published.HasValue && v.Downloads.HasValue)
            .GroupBy(v => new DateTime(v.Published!.Value.Year, v.Published.Value.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Month = g.Key.ToString("MMM yyyy"),
                Downloads = g.Sum(v => (double)v.Downloads!.Value)
            })
            .ToList();

        // Calculate average downloads per month (stable value for all months)
        var averageDownloads = monthlyChartData.Count > 0
            ? Math.Round(monthlyChartData.Average(m => m.Downloads))
            : 0.0;

        // Add average to each month
        var monthlyChartDataWithAverage = monthlyChartData
            .Select(m => new
            {
                m.Month,
                m.Downloads,
                Average = averageDownloads
            })
            .ToList();

        // Find latest version with downloads
        var latestVersionInfo = s.Versions.FirstOrDefault(v => v.Version == s.LatestVersion);
        
        // Enhanced KPI cards with animated values
        var metrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(animatedDownloads.Value.ToString("N0")).Bold()
                    | (downloadsThisMonth > 0
                        ? Text.Block($"+{downloadsThisMonth:N0} this month").Muted()
                        : null)
            ).Title("Total Downloads").Icon(Icons.Download)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(animatedVersions.Value.ToString("N0")).Bold()
                    | (versionsThisMonth > 0
                        ? Text.Block($"+{versionsThisMonth} this month").Muted()
                        : null)
            ).Title("Total Versions").Icon(Icons.Tag)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(s.LatestVersion).Bold()
                    | (latestVersionInfo != null && latestVersionInfo.Downloads.HasValue && latestVersionInfo.Downloads.Value > 0
                        ? Text.Block($"{latestVersionInfo.Downloads.Value:N0} downloads").Muted()
                        : null)
            ).Title("Latest Version").Icon(Icons.ArrowUp)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(mostDownloadedVersion != null 
                        ? mostDownloadedVersion.Version 
                        : "N/A").Bold()
                    | (mostDownloadedVersion != null && mostDownloadedVersion.Downloads.HasValue && mostDownloadedVersion.Downloads.Value > 0
                        ? Text.Block($"{mostDownloadedVersion.Downloads.Value:N0} downloads").Muted()
                        : null)
            ).Title("Most Popular").Icon(Icons.Star);

        // Top 3 Popular Versions bar chart
        var topVersionsData = s.Versions
            .Where(v => v.Downloads.HasValue && v.Downloads.Value > 0)
            .OrderByDescending(v => v.Downloads)
            .Take(3)
            .Select(v => new
            {
                Version = v.Version,
                Downloads = (double)v.Downloads!.Value
            })
            .ToList();

        var topVersionsChart = topVersionsData.Count > 0
            ? topVersionsData.ToBarChart()
                .Dimension("Version", e => e.Version)
                .Measure("Downloads", e => e.Sum(f => f.Downloads))
            : null;

        var adoptionCard = topVersionsChart != null
            ? new Card(
                Layout.Vertical().Gap(3).Padding(3)
                    | topVersionsChart
            ).Title("Top Popular Versions").Icon(Icons.Star).Height(Size.Full())
            : new Card(
                Layout.Vertical().Gap(3).Padding(3).Align(Align.Center)
                    | Text.Block("No download data available").Muted()
            ).Title("Top Popular Versions").Icon(Icons.Star).Height(Size.Full());

        // Monthly Downloads Card - compares this month with average
        var percentDiff = avgMonthlyDownloads > 0
            ? Math.Round(((downloadsThisMonth - avgMonthlyDownloads) / avgMonthlyDownloads) * 100, 1)
            : 0.0;

        var isGrowing = downloadsThisMonth > avgMonthlyDownloads;
        var trendText = percentDiff == 0 
            ? "On track"
            : isGrowing 
                ? $"+{Math.Abs(percentDiff):F1}% vs avg"
                : $"{percentDiff:F1}% vs avg";

        // Monthly downloads line chart with average line
        var monthlyDownloadsChart = monthlyChartDataWithAverage
            .ToLineChart(
                dimension: m => m.Month,
                measures: [
                    m => m.First().Downloads,
                    m => m.First().Average
                ],
                LineChartStyles.Dashboard);

        var monthlyDownloadsCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | monthlyDownloadsChart
        ).Title("Monthly Downloads")
         .Icon(isGrowing ? Icons.TrendingUp : Icons.TrendingDown)
         .Height(Size.Full());


        // Version distribution chart - enhanced with filters
        // Use filtered data from UseQuery which automatically updates when filters change
        // Shows versions with most downloads (same approach as Top Popular Versions)
        var versionChartData = filteredVersionChartQuery.Value ?? new List<VersionChartDataItem>();
        var versionChartDataForChart = versionChartData
            .Select(v => new 
            { 
                Version = v.Version, 
                Downloads = v.Downloads
            })
            .ToList();

        var versionChart = versionChartDataForChart.Count > 0
            ? versionChartDataForChart.ToBarChart()
                .Dimension("Version", e => e.Version)
                .Measure("Downloads", e => e.Sum(f => f.Downloads))
            : null;

        var versionChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Layout.Horizontal().Gap(2)
                    | Text.H4("Recent Versions Distribution")
                | (Layout.Horizontal().Gap(2).Align(Align.Center)
                    | versionChartFromDate.ToDateInput().WithField()
                    | versionChartToDate.ToDateInput().WithField()
                    | new Button(versionChartShowPreReleases.Value ? "With Pre-releases" : "Releases Only")
                        .Outline()
                        .Icon(Icons.ChevronDown)
                        .WithDropDown(
                            MenuItem.Default("With Pre-releases").HandleSelect(() => versionChartShowPreReleases.Set(true)),
                            MenuItem.Default("Releases Only").HandleSelect(() => versionChartShowPreReleases.Set(false))
                        )
                    | new NumberInput<int>(versionChartCount)
                        .Min(2)
                        .Max(20)
                        .Width(Size.Units(60)))
                | (versionChart != null
                    ? versionChart
                    : Text.Block("No versions found").Muted())
        );

        // Version timeline chart - group by month to show all releases
        // Include ALL versions with valid dates (filter only invalid dates, not by year range)
        var timelineData = s.Versions
            .Where(v => v.Published.HasValue && v.Published.Value.Year >= 2000)
            .Select(v => new { 
                YearMonth = new DateTime(v.Published!.Value.Year, v.Published.Value.Month, 1),
                OriginalDate = v.Published.Value
            })
            .GroupBy(v => v.YearMonth)
            .Select(g => new { 
                Date = g.Key, 
                Releases = (double)g.Count() 
            })
            .OrderBy(v => v.Date)
            .ToList();

        var timelineChart = timelineData.ToLineChart(
            dimension: e => e.Date.ToString("MMM yyyy"),
            measures: [e => e.Sum(f => f.Releases)],
            LineChartStyles.Dashboard);

        var timelineChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Version Releases Over Time")
                | timelineChart
        );

        // Releases vs Pre-releases pie chart
        var releasesCount = s.Versions.Count(v => !IsPreRelease(v.Version));
        var preReleasesCount = s.Versions.Count(v => IsPreRelease(v.Version));
        
        var releaseTypeData = new[]
        {
            new { Type = "Releases", Count = releasesCount },
            new { Type = "Pre-releases", Count = preReleasesCount }
        }.Where(x => x.Count > 0).ToList();

        var releaseTypePieChart = releaseTypeData.Count > 0
            ? releaseTypeData.ToPieChart(
                dimension: item => item.Type,
                measure: item => item.Sum(x => (double)x.Count),
                PieChartStyles.Dashboard,
                new PieChartTotal(s.Versions.Count.ToString("N0"), "Total Versions"))
            : null;
        var releaseTypeChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | (releaseTypePieChart ?? (object)Text.Block("No data available").Muted())
            ).Title("Releases vs Pre-releases");

        // All versions table - use ALL versions from service (no filtering)
        var allVersionsTable = s.Versions
            .Select(v => new
            {
                Version = v.Version,
                Published = v.Published.HasValue ? v.Published.Value.ToString("MMM dd, yyyy") : "N/A",
                Downloads = v.Downloads.HasValue ? v.Downloads.Value.ToString("N0") : "N/A"
            })
            .ToList();

        var versionsTable = allVersionsTable.AsQueryable()
            .ToDataTable()
            .Height(Size.Units(150))
            .Header(v => v.Version, "Version")
            .Header(v => v.Published, "Published")
            .Header(v => v.Downloads, "Downloads")
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;
            });

        var versionsTableCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Text.H4($"All Versions ({allVersionsTable.Count})")
                | versionsTable
        ).Width(Size.Fraction(0.6f));

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Grid().Columns(3).Gap(3).Width(Size.Fraction(0.9f))
                | adoptionCard
                | monthlyDownloadsCard
                | releaseTypeChartCard)
            | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                | versionsTableCard
                | (Layout.Vertical().Width(Size.Full())
                    | versionChartCard
                    | timelineChartCard ));
    }
}
