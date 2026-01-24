using IvyInsights.Models;
using IvyInsights.Services;
using Ivy.Helpers;

namespace IvyInsights.Apps;

[App(icon: Icons.ChartBar, title: "NuGet Statistics")]
public class NuGetStatsApp : ViewBase
{
    private const string PackageId = "Ivy";

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
        var animatedAdoptionRate = this.UseState(0.0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        // Animate numbers when data is loaded
        if (statsQuery.Value != null && !hasAnimated.Value)
        {
            var animStats = statsQuery.Value;
            var animTotalDownloads = animStats.TotalDownloads ?? 0;
            var animTotalVersions = animStats.TotalVersions;
            
            // Calculate adoption rate (percentage of users on recent versions)
            var animRecentVersions = animStats.Versions
                .Where(v => v.Published.HasValue && v.Published.Value >= DateTime.UtcNow.AddMonths(-6))
                .ToList();
            var animRecentDownloads = animRecentVersions
                .Where(v => v.Downloads.HasValue)
                .Sum(v => v.Downloads!.Value);
            var animTotalDownloadsWithData = animStats.Versions
                .Where(v => v.Downloads.HasValue)
                .Sum(v => v.Downloads!.Value);
            var animAdoptionRate = animTotalDownloadsWithData > 0 
                ? Math.Round((animRecentDownloads / (double)animTotalDownloadsWithData) * 100, 1)
                : 0.0;

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
                        animatedAdoptionRate.Set(animAdoptionRate * currentProgress);
                        refresh.Refresh();
                        progress.Report(currentProgress);
                        await Task.Delay(delayMs, token);
                    }
                    animatedDownloads.Set(animTotalDownloads);
                    animatedVersions.Set(animTotalVersions);
                    animatedAdoptionRate.Set(animAdoptionRate);
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
                | (Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(0.8f))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.8f))
                    | new Skeleton().Height(Size.Units(200))
                    | new Skeleton().Height(Size.Units(200)));
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

        // Calculate adoption rate
        var recentVersions = s.Versions
            .Where(v => v.Published.HasValue && v.Published.Value >= DateTime.UtcNow.AddMonths(-6))
            .ToList();
        var recentDownloads = recentVersions
            .Where(v => v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);
        var totalDownloadsWithData = s.Versions
            .Where(v => v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);
        var adoptionRate = totalDownloadsWithData > 0 
            ? Math.Round((recentDownloads / (double)totalDownloadsWithData) * 100, 1)
            : 0.0;

        // Calculate growth metrics
        var versionsLastMonth = s.Versions
            .Count(v => v.Published.HasValue && v.Published.Value >= DateTime.UtcNow.AddMonths(-1));
        var versionsLast3Months = s.Versions
            .Count(v => v.Published.HasValue && v.Published.Value >= DateTime.UtcNow.AddMonths(-3));
        var avgReleasesPerMonth = versionsLast3Months / 3.0;
        
        // Calculate downloads for versions published in the last month
        var downloadsLastMonth = s.Versions
            .Where(v => v.Published.HasValue && v.Published.Value >= DateTime.UtcNow.AddMonths(-1) && v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);

        // Smart Insights generation
        var insights = new List<string>();
        if (adoptionRate > 50)
            insights.Add($"Strong adoption: {adoptionRate}% of users are on recent versions (last 6 months)");
        else if (adoptionRate > 30)
            insights.Add($"Growing adoption: {adoptionRate}% of users migrated to recent versions");
        else
            insights.Add($"Opportunity: Only {adoptionRate}% on recent versions - consider migration incentives");

        if (avgReleasesPerMonth > 2)
            insights.Add($"Active development: {avgReleasesPerMonth:F1} releases per month on average");
        else if (avgReleasesPerMonth > 0.5)
            insights.Add($"Steady releases: Consistent updates every ~{Math.Round(1 / avgReleasesPerMonth, 1)} months");
        else
            insights.Add($"Stable package: Focused on quality over frequency");

        if (mostDownloadedVersion != null && mostDownloadedVersion.Downloads.HasValue)
        {
            var mostPopularShare = totalDownloadsWithData > 0
                ? Math.Round((mostDownloadedVersion.Downloads.Value / (double)totalDownloadsWithData) * 100, 1)
                : 0;
            if (mostPopularShare > 30)
                insights.Add($"Version {mostDownloadedVersion.Version} dominates with {mostPopularShare}% of all downloads");
        }

        // Find latest version with downloads
        var latestVersionInfo = s.Versions.FirstOrDefault(v => v.Version == s.LatestVersion);
        
        // Enhanced KPI cards with animated values
        var metrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(animatedDownloads.Value.ToString("N0")).Bold()
                    | (downloadsLastMonth > 0
                        ? Text.Block($"+{downloadsLastMonth:N0} this month").Muted()
                        : null)
            ).Title("Total Downloads").Icon(Icons.Download)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3).Align(Align.Center)
                    | Text.H2(animatedVersions.Value.ToString("N0")).Bold()
                    | (versionsLastMonth > 0
                        ? Text.Block($"+{versionsLastMonth} this month").Muted()
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

        // Smart Insights card
        var insightsCard = new Card(
            Layout.Vertical().Gap(2).Padding(3)
                | Text.H4("Smart Insights").Bold()
                | Layout.Vertical().Gap(2)
                    | insights.Select(insight => 
                        Layout.Horizontal().Gap(2).Align(Align.TopLeft)
                            | Text.Block("•").Muted()
                            | Text.Block(insight).Width(Size.Fraction(0.95f))
                    )
        ).Width(Size.Fraction(0.35f));

        // Adoption Rate card
        var adoptionCard = new Card(
            Layout.Vertical().Gap(3).Padding(3).Align(Align.Center)
                | Text.H3($"{animatedAdoptionRate.Value:F1}%").Bold()
                | Text.Muted("Adoption Rate")
                | Text.Block("Users on versions from last 6 months").Muted()
                | (adoptionRate > 50
                    ? Text.Block("Excellent adoption")
                    : adoptionRate > 30
                        ? Text.Block("Good momentum")
                        : Text.Block("Growth opportunity"))
        ).Title("Adoption Velocity").Icon(Icons.TrendingUp).Width(Size.Fraction(0.3f));

        // Package info card
        var packageInfo = new Card(
            Layout.Vertical().Gap(2).Padding(3)
                | Text.H3("Package Information")
                | (s.Description != null ? Text.Block(s.Description) : Text.Muted("No description available"))
                | (s.Authors != null ? Text.Block($"Authors: {s.Authors}") : null)
                | (s.ProjectUrl != null 
                    ? Text.Markdown($"Project URL: [{s.ProjectUrl}]({s.ProjectUrl})")
                    : null)
                | Text.Block($"First Published: {(s.FirstVersionPublished.HasValue ? s.FirstVersionPublished.Value.ToString("MMM dd, yyyy") : "N/A")}")
        ).Width(Size.Fraction(0.35f));

        // Version distribution chart - enhanced with top version highlight
        var versionChartData = s.Versions
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .Take(20)
            .Select(v => new 
            { 
                Version = v.Version, 
                Downloads = (double)(v.Downloads ?? 0),
                HasDownloads = v.Downloads.HasValue && v.Downloads.Value > 0,
                IsTopVersion = mostDownloadedVersion != null && v.Version == mostDownloadedVersion.Version
            })
            .ToList();

        var versionChart = versionChartData.ToBarChart()
            .Dimension("Version", e => e.Version)
            .Measure("Downloads", e => e.Sum(f => f.HasDownloads ? f.Downloads : 1.0));

        var versionChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Layout.Horizontal().Gap(2)
                    | Text.H4("Recent Versions Distribution")
                    | (mostDownloadedVersion != null && mostDownloadedVersion.Downloads.HasValue
                        ? Layout.Horizontal().Gap(1).Align(Align.Center)
                            | Text.Block("Top: ").Muted()
                            | Text.Block(mostDownloadedVersion.Version).Bold()
                            | Text.Block($" ({mostDownloadedVersion.Downloads.Value:N0})").Muted()
                        : null)
                | Text.Muted(versionsWithDownloads > 0 
                    ? "Showing downloads per version (hover for details)" 
                    : "Showing version count (download data not available)")
                | versionChart
        ).Width(Size.Fraction(0.65f));

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
        ).Width(Size.Fraction(0.65f));

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
            .Header(v => v.Version, "Version")
            .Header(v => v.Published, "Published")
            .Header(v => v.Downloads, "Downloads")
            .Height(Size.Units(400))
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
        ).Width(Size.Fraction(0.35f));

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                | adoptionCard
                | insightsCard
                | packageInfo)
            | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                | versionChartCard
                | timelineChartCard)
            | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                | versionsTableCard
                | new Spacer());
    }
}
