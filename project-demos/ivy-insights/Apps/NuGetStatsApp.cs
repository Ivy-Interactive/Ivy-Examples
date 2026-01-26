using IvyInsights.Models;
using IvyInsights.Services;
using Ivy.Helpers;

namespace IvyInsights.Apps;

internal class VersionChartDataItem
{
    public string Version { get; set; } = string.Empty;
    public double Downloads { get; set; }
}

[App(icon: Icons.ChartBar, title: "Ivy Statistics")]
public class IvyInsightsApp : ViewBase
{
    private const string PackageId = "Ivy";

    private static bool IsPreRelease(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;
        
        var parts = version.Split('-');
        return parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]);
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var nugetProvider = UseService<INuGetStatisticsProvider>();
        
        var statsQuery = this.UseQuery(
            key: $"nuget-stats/{PackageId}",
            fetcher: async (CancellationToken ct) =>
            {
                var statistics = await nugetProvider.GetPackageStatisticsAsync(PackageId, ct);
                
                try
                {
                    client.Toast($"Successfully loaded statistics for {PackageId}!");
                }
                catch { }
                
                return statistics;
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(15),
                KeepPrevious = true,
                RevalidateOnMount = true
            },
            tags: ["nuget", "statistics"]);
        
        var animatedDownloads = this.UseState(0L);
        var animatedVersions = this.UseState(0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        var versionChartFromDate = this.UseState<DateTime?>(() => null);
        var versionChartToDate = this.UseState<DateTime?>(() => null);
        var versionChartShowPreReleases = this.UseState(true);
        var versionChartCount = this.UseState(7);

        var dbService = UseService<IDatabaseService>();
        var dailyStatsQuery = this.UseQuery(
            key: "daily-download-stats",
            fetcher: async (CancellationToken ct) =>
            {
                return await dbService.GetDailyDownloadStatsAsync(30, ct);
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(5),
                KeepPrevious = true,
                RevalidateOnMount = true
            },
            tags: ["database", "downloads"]);

        var filteredVersionChartQuery = this.UseQuery(
            key: $"version-chart-filtered/{PackageId}/{statsQuery.Value != null}/{versionChartFromDate.Value?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartToDate.Value?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartShowPreReleases.Value}/{versionChartCount.Value}",
            fetcher: async (CancellationToken ct) =>
            {
                if (statsQuery.Value == null)
                    return new List<VersionChartDataItem>();

                var s = statsQuery.Value;
                var count = Math.Clamp(versionChartCount.Value, 2, 20);
                var filteredVersions = s.Versions.AsEnumerable();
                
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
                
                if (!versionChartShowPreReleases.Value)
                {
                    filteredVersions = filteredVersions.Where(v => !IsPreRelease(v.Version));
                }
                
                filteredVersions = filteredVersions.Where(v => v.Downloads.HasValue && v.Downloads.Value > 0);
                
                var versionChartData = filteredVersions
                    .OrderByDescending(v => v.Downloads)
                    .Take(count)
                    .Select(v => new VersionChartDataItem
                    { 
                        Version = v.Version, 
                        Downloads = (double)v.Downloads!.Value
                    })
                    .ToList();

                return versionChartData;
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.Zero,
                KeepPrevious = false,
                RevalidateOnMount = false
            });

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

        var mostDownloadedVersion = s.Versions
            .Where(v => v.Downloads.HasValue && v.Downloads.Value > 0)
            .OrderByDescending(v => v.Downloads)
            .FirstOrDefault();
        
        if (mostDownloadedVersion == null)
        {
            mostDownloadedVersion = s.Versions
                .OrderByDescending(v => v.Published ?? DateTime.MinValue)
                .FirstOrDefault();
        }


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
        
        var downloadsLastMonth = s.Versions
            .Where(v => v.Published.HasValue && 
                       v.Published.Value >= lastMonthStart && 
                       v.Published.Value < lastMonthEnd &&
                       v.Downloads.HasValue)
            .Sum(v => v.Downloads!.Value);
        
        // Use daily stats from database
        var dailyStats = dailyStatsQuery.Value ?? new List<DailyDownloadStats>();
        
        // Get daily downloads for the last month (last 30 days)
        var last30DaysStart = now.AddDays(-30).Date;
        var dailyChartData = dailyStats
            .Where(d => d.Date >= DateOnly.FromDateTime(last30DaysStart))
            .OrderBy(d => d.Date)
            .Select(d => new
            {
                Date = d.Date.ToString("MMM dd"),
                Downloads = (double)Math.Max(0, d.DailyGrowth)
            })
            .ToList();

        var averageDailyDownloads = dailyChartData.Count > 0
            ? Math.Round(dailyChartData.Average(d => d.Downloads))
            : 0.0;

        var dailyChartDataWithAverage = dailyChartData
            .Select(d => new
            {
                d.Date,
                d.Downloads,
                Average = averageDailyDownloads
            })
            .ToList();

        // Calculate this month and average for metrics using database data
        var downloadsThisMonth = dailyStats
            .Where(d => d.Date.Year == now.Year && d.Date.Month == now.Month)
            .Sum(d => Math.Max(0, d.DailyGrowth));
        
        var avgMonthlyDownloads = dailyChartData.Count > 0 
            ? dailyChartData.Average(d => d.Downloads) * 30 // Average daily * 30 days
            : 0.0;

        var latestVersionInfo = s.Versions.FirstOrDefault(v => v.Version == s.LatestVersion);
        
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

        var percentDiff = avgMonthlyDownloads > 0
            ? Math.Round(((downloadsThisMonth - avgMonthlyDownloads) / avgMonthlyDownloads) * 100, 1)
            : 0.0;

        var isGrowing = downloadsThisMonth > avgMonthlyDownloads;

        var dailyDownloadsChart = dailyChartDataWithAverage.Count > 0
            ? dailyChartDataWithAverage
                .ToLineChart(
                    dimension: d => d.Date,
                    measures: [
                        d => d.First().Downloads,
                        d => d.First().Average
                    ],
                    LineChartStyles.Dashboard)
            : null;

        var monthlyDownloadsCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | (dailyDownloadsChart != null
                    ? dailyDownloadsChart
                    : Text.Block("No data available for the last month").Muted())
        ).Title("Daily Downloads (Last 30 Days)")
         .Icon(isGrowing ? Icons.TrendingUp : Icons.TrendingDown)
         .Height(Size.Full());


        var versionChartData = filteredVersionChartQuery.Value ?? new List<VersionChartDataItem>();

        var versionChart = versionChartData.Count > 0
            ? versionChartData.ToBarChart()
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

        var timelineData = s.Versions
            .Where(v => v.Published.HasValue && v.Published.Value.Year >= 2000)
            .GroupBy(v => new DateTime(v.Published!.Value.Year, v.Published.Value.Month, 1))
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
