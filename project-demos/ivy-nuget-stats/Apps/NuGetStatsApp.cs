using IvyInsights.Models;
using IvyInsights.Services;
using Ivy.Helpers;
using Ivy.Widgets.ActivityHeatmap;

namespace IvyInsights.Apps;

internal class VersionChartDataItem
{
    public string Version { get; set; } = string.Empty;
    public double Downloads { get; set; }
}

internal class StargazersDailyChartData
{
    public string Date { get; set; } = string.Empty;
    public double NewCount { get; set; }
    public double UnstarCount { get; set; }
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

    private static List<DailyDownloadStats> CreateMockDailyDownloadStats(int days)
    {
        var stats = new List<DailyDownloadStats>(days);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var random = new Random(42);
        var totalDownloads = 65_000L;

        for (var i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dailyGrowth = random.Next(5, 520);
            totalDownloads += dailyGrowth;
            stats.Add(new DailyDownloadStats
            {
                Date = date,
                TotalDownloads = totalDownloads,
                DailyGrowth = dailyGrowth
            });
        }

        return stats;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var navigator = UseNavigation();
        var nugetProvider = UseService<INuGetStatisticsProvider>();
        var updateService = UseService<IDatabaseUpdateService>();

        var statsQuery = this.UseQuery(
            key: $"nuget-stats/{PackageId}",
            fetcher: async (CancellationToken ct) =>
            {
                var statistics = await nugetProvider.GetPackageStatisticsAsync(PackageId, ct);
                client.Toast($"Successfully loaded statistics for {PackageId}!");
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
        var isUpdatingDatabase = this.UseState(false);
        var stargazerRefreshVersion = this.UseState(0);
        var showStargazersTodayDialog = this.UseState(false);
        var stargazersDateRange = this.UseState<(DateOnly, DateOnly)>(() =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return (today, today);
        });
        var selectedStargazer = this.UseState<GithubStargazer?>(() => null);
        var cachedStargazers = this.UseState<List<GithubStargazer>>(() => new List<GithubStargazer>());

        var versionChartDateRange = this.UseState<(DateOnly?, DateOnly?)>(() => (
            DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            DateOnly.FromDateTime(DateTime.Today)));
        var versionChartShowPreReleases = this.UseState(true);
        var versionChartCount = this.UseState(7);

        var dbService = UseService<IDatabaseService>();
        var dailyStatsQuery = this.UseQuery(
            key: "daily-download-stats-90",
            fetcher: async (CancellationToken ct) =>
            {
                return await dbService.GetDailyDownloadStatsAsync(90, ct);
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(5),
                KeepPrevious = true,
                RevalidateOnMount = true
            },
            tags: ["database", "downloads"]);

        var starsStatsQuery = this.UseQuery(
            key: "github-stars-stats",
            fetcher: async (CancellationToken ct) =>
            {
                return await dbService.GetGithubStarsStatsAsync(365, ct);
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(5),
                KeepPrevious = true,
                RevalidateOnMount = true
            },
            tags: ["database", "stars"]);


        var totalDownloadsStatsQuery = this.UseQuery(
            key: "total-downloads-stats-365",
            fetcher: async (CancellationToken ct) =>
            {
                return await dbService.GetDailyDownloadStatsAsync(365, ct);
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(5),
                KeepPrevious = true,
                RevalidateOnMount = true
            },
            tags: ["database", "downloads", "total"]);

        var stargazersQuery = this.UseQuery(
            key: $"github-stargazers-list/{stargazerRefreshVersion.Value}",
            fetcher: async (CancellationToken ct) =>
            {
                return await dbService.GetGithubStargazersAsync(ct);
            },
            options: new QueryOptions
            {
                Scope = QueryScope.Server,
                Expiration = TimeSpan.FromMinutes(15),
                KeepPrevious = true,
                RevalidateOnMount = false
            },
            tags: ["database", "stargazers", "list"]);

        // Cache stargazers data so old data stays visible while a new query loads
        if (stargazersQuery.Value is { Count: > 0 } freshStargazers && !ReferenceEquals(freshStargazers, cachedStargazers.Value))
        {
            cachedStargazers.Set(freshStargazers);
        }

        var filteredVersionChartQuery = this.UseQuery(
            key: $"version-chart-filtered/{PackageId}/{statsQuery.Value != null}/{versionChartDateRange.Value.Item1?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartDateRange.Value.Item2?.ToString("yyyy-MM-dd") ?? "null"}/{versionChartShowPreReleases.Value}/{versionChartCount.Value}",
            fetcher: (CancellationToken ct) =>
            {
                if (statsQuery.Value == null)
                    return Task.FromResult(new List<VersionChartDataItem>());

                var s = statsQuery.Value;
                var count = Math.Clamp(versionChartCount.Value, 2, 20);
                var filteredVersions = s.Versions.AsEnumerable();

                if (versionChartDateRange.Value.Item1.HasValue)
                {
                    var fromDate = versionChartDateRange.Value.Item1.Value.ToDateTime(TimeOnly.MinValue);
                    filteredVersions = filteredVersions.Where(v =>
                        v.Published.HasValue && v.Published.Value.Date >= fromDate);
                }
                if (versionChartDateRange.Value.Item2.HasValue)
                {
                    var toDate = versionChartDateRange.Value.Item2.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
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

                return Task.FromResult(versionChartData);
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
                    Layout.Vertical()
                        | Text.H3("Error")
                        | Text.Block(error.Message)
                        | new Button("Retry", onClick: _ => statsQuery.Mutator.Revalidate())
                            .Icon(Icons.RefreshCcw)
                ).Width(Size.Fraction(0.5f));
        }

        if (statsQuery.Loading && statsQuery.Value == null)
        {
            return Layout.Vertical().AlignContent(Align.TopCenter).Gap(2)
                | Text.H1("NuGet Statistics")
                | Text.Muted($"Loading statistics for {PackageId}...")
                | (Layout.Grid().Columns(5).Width(Size.Fraction(0.9f))
                    | new Skeleton().Height(Size.Units(50))
                    | new Skeleton().Height(Size.Units(50))
                    | new Skeleton().Height(Size.Units(50))
                    | new Skeleton().Height(Size.Units(50))
                    | new Skeleton().Height(Size.Units(50)))
                | (Layout.Grid().Columns(3).Width(Size.Fraction(0.9f))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | (Layout.Horizontal().Gap(2).Width(Size.Fraction(0.9f))
                    | new Skeleton().Width(Size.Fraction(0.5f)).Height(Size.Units(80))
                    | new Skeleton().Width(Size.Fraction(0.5f)).Height(Size.Units(80)))
                | (Layout.Horizontal().Gap(2).Width(Size.Fraction(0.9f))
                    | new Skeleton().Width(Size.Fraction(0.5f)).Height(Size.Units(80))
                    | new Skeleton().Width(Size.Fraction(0.5f)).Height(Size.Units(80)))
                | new Skeleton().Width(Size.Fraction(0.9f)).Height(Size.Units(130));
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

        var versionsThisMonth = s.Versions
            .Count(v => v.Published.HasValue &&
                       v.Published.Value >= thisMonthStart &&
                       v.Published.Value < now);

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

        // Calculate 5-day moving average for each day
        var dailyChartDataWithAverage = dailyChartData
            .Select((d, index) =>
            {
                var movingAverage = 0.0;

                if (index >= 4)
                {
                    // We have at least 5 days, calculate average of last 5 days
                    // (current day + previous 4 days = 5 days total)
                    var last5Days = dailyChartData
                        .Skip(index - 4)  // Skip to 4 days before current
                        .Take(5)          // Take 5 days total
                        .Select(x => x.Downloads)
                        .ToList();
                    movingAverage = Math.Round(last5Days.Average(), 1);
                }
                else
                {
                    // Less than 5 days available, calculate average of all days up to this point
                    var availableDays = dailyChartData
                        .Take(index + 1)  // Take all days from start to current
                        .Select(x => x.Downloads)
                        .ToList();
                    movingAverage = Math.Round(availableDays.Average(), 1);
                }

                return new
                {
                    d.Date,
                    Downloads = d.Downloads,
                    movingAverage = movingAverage
                };
            })
            .ToList();

        // Calculate growth using last 14 days (This Week vs Previous Week) based on Dates
        var today = DateOnly.FromDateTime(now);
        var startOfThisWeek = today.AddDays(-6);
        var startOfPrevWeek = startOfThisWeek.AddDays(-7);

        var thisWeekDownloads = dailyStats
            .Where(d => d.Date >= startOfThisWeek && d.Date <= today)
            .Sum(d => Math.Max(0, d.DailyGrowth));

        var prevWeekDownloads = dailyStats
            .Where(d => d.Date >= startOfPrevWeek && d.Date < startOfThisWeek)
            .Sum(d => Math.Max(0, d.DailyGrowth));

        var growthPercent = 0.0;
        if (prevWeekDownloads > 0)
        {
            growthPercent = ((double)(thisWeekDownloads - prevWeekDownloads) / prevWeekDownloads) * 100;
        }
        else if (thisWeekDownloads > 0)
        {
            // If previous week is 0, show the current count as percentage growth (e.g. 0 -> 404 = +404%)
            growthPercent = (double)thisWeekDownloads;
        }

        var latestVersionInfo = s.Versions.FirstOrDefault(v => v.Version == s.LatestVersion);

        var trendIcon = growthPercent >= 0 ? Icons.TrendingUp : Icons.TrendingDown;
        var trendColor = growthPercent >= 0 ? Colors.Success : Colors.Destructive;

        var topVersionsData = s.Versions
            .Where(v => v.Published.HasValue && v.Published.Value >= now.AddDays(-30))
            .Where(v => v.Downloads.HasValue && v.Downloads.Value > 0)
            .OrderByDescending(v => v.Downloads)
            .Take(5)
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
                Layout.Vertical()
                    | topVersionsChart
            ).Title("Top Popular Versions (Last 30 Days)").Icon(Icons.Crown).Height(Size.Full())
            : new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.Block("No versions released in the last 30 days").Muted()
            ).Title("Top Popular Versions (Last 30 Days)").Icon(Icons.Crown).Height(Size.Full());

        var dailyDownloadsChart = dailyChartDataWithAverage.Count > 0
            ? dailyChartDataWithAverage
                .ToLineChart(
                    dimension: d => d.Date,
                    measures: [
                        d => d.First().Downloads,
                    ],
                    LineChartStyles.Dashboard)
                .Measure("Moving Average", d => d.First().movingAverage)
            : null;

        var monthlyDownloadsCard = new Card(
            Layout.Vertical()
                | (dailyDownloadsChart != null
                    ? dailyDownloadsChart
                    : Text.Block("No data available for the last 90 days").Muted())
        ).Title("Daily Downloads (Last 90 Days)")
         .Icon(Icons.ChartNoAxesCombined)
         .Height(Size.Full());


        var versionChartData = filteredVersionChartQuery.Value ?? new List<VersionChartDataItem>();

        var versionChart = versionChartData.Count > 0
            ? versionChartData.ToBarChart()
                .Dimension("Version", e => e.Version)
                .Measure("Downloads", e => e.Sum(f => f.Downloads))
            : null;

        var versionChartCard = new Card(
            Layout.Horizontal().Height(Size.Full())
                 | (Layout.Vertical()
                    | (Layout.Horizontal().AlignContent(Align.Center).Height(Size.Fit())
                        | versionChartDateRange.ToDateRangeInput()
                            .Format("MMM dd, yyyy")
                            .Placeholder("Select date range")
                        | new Button(versionChartShowPreReleases.Value ? "With Pre-releases" : "Releases Only")
                            .Outline()
                            .Icon(Icons.ChevronDown)
                            .WithDropDown(
                                MenuItem.Default("With Pre-releases").OnSelect(() => versionChartShowPreReleases.Set(true)),
                                MenuItem.Default("Releases Only").OnSelect(() => versionChartShowPreReleases.Set(false))
                            )
                        | versionChartCount.ToNumberInput()
                            .Min(2)
                            .Max(20)
                            .Width(Size.Units(60)))
                    | (versionChart != null
                        ? versionChart
                        : Text.Block("No versions found").Muted())))
            .Title("Recent Versions Distribution")
            .Icon(Icons.ChartBar)
            .Height(Size.Full());

        var starsStats = starsStatsQuery.Value ?? new List<GithubStarsStats>();

        var latestStarsEntry = starsStats
            .OrderByDescending(d => d.Date)
            .FirstOrDefault();
        var currentStars = latestStarsEntry?.Stars ?? 0L;

        var starsMonthStart = new DateOnly(now.Year, now.Month, 1);
        var starsAtMonthStart = starsStats
            .Where(d => d.Date <= starsMonthStart)
            .OrderByDescending(d => d.Date)
            .FirstOrDefault()?.Stars ?? 0L;
        var starsThisMonth = currentStars - starsAtMonthStart;

        var starsChartData = starsStats
            .OrderBy(d => d.Date)
            .Select(d => new
            {
                Date = d.Date.ToString("MMM dd"),
                Stars = (double)d.Stars
            })
            .ToList();

        var starsChart = starsChartData.Count > 0
            ? starsChartData.ToLineChart(
                dimension: e => e.Date,
                measures: [e => e.First().Stars],
                LineChartStyles.Dashboard)
            : null;

        var githubStarsCard = new Card(
            Layout.Horizontal().Height(Size.Full())
                | (starsChart != null
                    ? starsChart
                    : (object)Text.Block("No data available").Muted())
        ).Title("GitHub Stars (Last 365 Days)").Icon(Icons.Github);

        var metrics = Layout.Grid().Columns(5)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | (Layout.Horizontal().AlignContent(Align.Center)
                        | Text.H2(animatedDownloads.Value.ToString("N0")).Bold()
                        | (thisWeekDownloads > 0 || prevWeekDownloads > 0
                            ? (Layout.Horizontal().Gap(1).Width(Size.Fit())
                                | new Icon(trendIcon).Color(trendColor)
                                | Text.H3($"{Math.Abs(growthPercent):0.0}%").Color(trendColor))
                            : null))
                    | Text.Block($"+{thisWeekDownloads:N0} this week").Muted()
            ).Title("Total Downloads").Icon(Icons.Download)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(animatedVersions.Value.ToString("N0")).Bold()
                    | Text.Block(versionsThisMonth > 0
                        ? $"+{versionsThisMonth} this month"
                        : "0 versions released this month").Muted()
            ).Title("Total Versions").Icon(Icons.Tag)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(s.LatestVersion).Bold()
                    | (latestVersionInfo != null && latestVersionInfo.Downloads.HasValue && latestVersionInfo.Downloads.Value > 0
                        ? Text.Block($"{latestVersionInfo.Downloads.Value:N0} downloads").Muted()
                        : null)
            ).Title("Latest Version").Icon(Icons.ArrowUp)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(mostDownloadedVersion != null
                        ? mostDownloadedVersion.Version
                        : "N/A").Bold()
                    | (mostDownloadedVersion != null && mostDownloadedVersion.Downloads.HasValue && mostDownloadedVersion.Downloads.Value > 0
                        ? Text.Block($"{mostDownloadedVersion.Downloads.Value:N0} downloads").Muted()
                        : null)
            ).Title("Most Popular").Icon(Icons.Star)
            | new Card(
                Layout.Vertical().AlignContent(Align.Center)
                    | Text.H2(currentStars.ToString("N0")).Bold()
                    | Text.Block(starsThisMonth > 0
                        ? $"+{starsThisMonth:N0} this month"
                        : starsThisMonth < 0
                            ? $"{starsThisMonth:N0} this month"
                            : "0 stars added this month").Muted()
            ).Title("GitHub Stars").Icon(Icons.Github)
             .OnClick(_ =>
             {
                 showStargazersTodayDialog.Set(true);
                 if (stargazersQuery.Value == null && !stargazersQuery.Loading)
                 {
                     stargazersQuery.Mutator.Revalidate();
                 }
             });

        var allStargazers = stargazersQuery.Value ?? cachedStargazers.Value;
        var last30Days = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

        // Group stargazers by the date they joined
        var joinedByDate = allStargazers
            .Where(sg => sg.StarredAt.HasValue)
            .GroupBy(sg => DateOnly.FromDateTime(sg.StarredAt!.Value))
            .Where(g => g.Key >= last30Days)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group stargazers by the date they left
        var leftByDate = allStargazers
            .Where(sg => sg.UnstarredAt.HasValue)
            .GroupBy(sg => DateOnly.FromDateTime(sg.UnstarredAt!.Value))
            .Where(g => g.Key >= last30Days)
            .ToDictionary(g => g.Key, g => g.Count());

        // Generate data for the last 30 days (including today)
        var stargazersChartData = new List<StargazersDailyChartData>();
        for (int i = 30; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
            var joined = joinedByDate.ContainsKey(date) ? joinedByDate[date] : 0;
            var left = leftByDate.ContainsKey(date) ? leftByDate[date] : 0;

            stargazersChartData.Add(new StargazersDailyChartData
            {
                Date = date.ToString("MMM dd"),
                NewCount = (double)joined,
                UnstarCount = (double)left
            });
        }

        var stargazersChart = stargazersChartData.Count > 0
            ? stargazersChartData.ToLineChart(
                dimension: e => e.Date,
                measures: [
                    e => e.First().NewCount,
                    e => e.First().UnstarCount
                ],
                LineChartStyles.Dashboard)
            : null;

        var stargazersDailyCard = new Card(
            Layout.Horizontal().Height(Size.Full())
                | (stargazersChart != null
                    ? stargazersChart
                    : (object)Text.Block("No data available").Muted())
        ).Title("Stargazers Daily (New vs Unstarred) - Last 30 Days").Icon(Icons.Users).Key($"stargazers-daily-card-{stargazerRefreshVersion.Value}");

        var totalDownloadsStats = totalDownloadsStatsQuery.Value ?? new List<DailyDownloadStats>();

        var totalDownloadsChartData = totalDownloadsStats
            .OrderBy(d => d.Date)
            .Select(d => new
            {
                Date = d.Date.ToString("MMM dd"),
                TotalDownloads = (double)d.TotalDownloads
            })
            .ToList();

        var totalDownloadsChart = totalDownloadsChartData.Count > 0
            ? totalDownloadsChartData.ToLineChart(
                dimension: e => e.Date,
                measures: [e => e.First().TotalDownloads],
                LineChartStyles.Dashboard)
            : null;

        var totalDownloadsCard = new Card(
            Layout.Horizontal().Height(Size.Full())
                | (totalDownloadsChart != null
                    ? totalDownloadsChart
                    : (object)Text.Block("No data available").Muted()))
            .Title("Total Downloads (Last 365 Days)")
            .Icon(Icons.Download)
            .Height(Size.Full());

#if DEBUG
        var useMockActivity = !totalDownloadsStatsQuery.Loading && totalDownloadsStats.Count == 0;
#else
        var useMockActivity = false;
#endif
        var dailyActivityData = (useMockActivity
            ? CreateMockDailyDownloadStats(365)
            : totalDownloadsStats).Select(d => new Activity
            {
                Date = d.Date,
                Count = Math.Max(0, (int)d.DailyGrowth)
            })
            .OrderBy(d => d.Date)
            .ToArray();

        var dailyActivityHeatmap = new ActivityHeatmap()
            .Data(dailyActivityData)
            .ColorScheme(Colors.Emerald);

        var dailyActivityCard = new Card(
            Layout.Horizontal().Height(Size.Full())
            | (dailyActivityData.Length > 0
                ? dailyActivityHeatmap
                : Text.Block("No data available").Muted()))
        .Title("Daily Growth (Last 365 days)" + (useMockActivity ? " (MOCK DATA)" : ""))
        .Icon(Icons.ArrowBigUpDash)
        .Height(Size.Full());

        // Calculate historical weekly growth for the chart
        var growthWeeks = new List<string>();
        var growthValues = new List<double>();
        var todayDate = DateOnly.FromDateTime(now);

        // Find the Monday of the current week to align strictly to Mon-Sun
        // If today is Sunday (0), we go back 6 days to Monday. Otherwise we go back DayOfWeek-1
        var diff = todayDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)todayDate.DayOfWeek - 1;
        var currentWeekMonday = todayDate.AddDays(-diff);

        // Go back 12 calendar weeks
        for (int i = 0; i < 12; i++)
        {
            var weekStart = currentWeekMonday.AddDays(-i * 7);
            var weekEnd = weekStart.AddDays(6); // Monday to Sunday
            var prevWeekStart = weekStart.AddDays(-7);

            var currentWeekSum = dailyStats
                .Where(d => d.Date >= weekStart && d.Date <= weekEnd)
                .Sum(d => Math.Max(0, d.DailyGrowth));

            var prevWeekSum = dailyStats
                .Where(d => d.Date >= prevWeekStart && d.Date < weekStart)
                .Sum(d => Math.Max(0, d.DailyGrowth));

            var growth = 0.0;
            if (prevWeekSum > 0)
            {
                growth = ((double)(currentWeekSum - prevWeekSum) / prevWeekSum) * 100;
            }
            else if (currentWeekSum > 0)
            {
                growth = (double)currentWeekSum;
            }

            growthWeeks.Add(weekStart.ToString("MM/dd"));
            growthValues.Add(growth);
        }

        // Reverse to show oldest to newest
        growthWeeks.Reverse();
        growthValues.Reverse();

        // Zero out the first data point with growth > 0 (outlier from 0→N downloads when history started)
        var firstPositiveIndex = growthValues.FindIndex(g => g > 0);
        if (firstPositiveIndex >= 0)
            growthValues[firstPositiveIndex] = 0;

        var weeklyGrowthData = growthWeeks.Zip(growthValues, (w, g) => new { Week = w, Growth = g })
            .ToList();

        var weeklyGrowthChart = weeklyGrowthData.Count > 0
            ? weeklyGrowthData.ToLineChart(
                dimension: d => d.Week,
                measures: [d => d.First().Growth],
                LineChartStyles.Dashboard)
            : null;

        var weeklyGrowthCard = new Card(
            Layout.Vertical()
                | (weeklyGrowthChart != null
                    ? weeklyGrowthChart
                    : (object)Text.Block("No history available").Muted()))
            .Title("Weekly Growth (WoW %)")
            .Icon(Icons.Activity)
            .Height(Size.Full());

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
            .Height(Size.Units(130))
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
            Layout.Vertical()
                | versionsTable
        ).Title($"All Versions ({allVersionsTable.Count})").Icon(Icons.List).Width(Size.Fraction(0.9f));

        Dialog? stargazersTodayDialog = null;

        // Stargazer activity by period dialog
        if (showStargazersTodayDialog.Value)
        {
            var (fromDate, toDate) = stargazersDateRange.Value;
            var stargazers = stargazersQuery.Value ?? cachedStargazers.Value;

            var periodEvents = stargazers
                .SelectMany(sg =>
                {
                    var list = new List<(string Username, string Action, string When, string DaysJoined)>();
                    if (sg.StarredAt is { } starred)
                    {
                        var d = DateOnly.FromDateTime(starred);
                        if (d >= fromDate && d <= toDate)
                        {
                            var days = sg.UnstarredAt.HasValue
                                ? (sg.UnstarredAt.Value.Date - starred.Date).Days
                                : (DateTime.UtcNow.Date - starred.Date).Days;

                            list.Add((sg.Username, "Joined", starred.ToString("yyyy-MM-dd HH:mm"), Math.Max(0, days).ToString()));
                        }
                    }
                    if (sg.UnstarredAt is { } unstarred)
                    {
                        var d = DateOnly.FromDateTime(unstarred);
                        if (d >= fromDate && d <= toDate)
                        {
                            var days = sg.StarredAt.HasValue
                                ? Math.Max(0, (unstarred.Date - sg.StarredAt.Value.Date).Days)
                                : 0;
                            list.Add((sg.Username, "Left", unstarred.ToString("yyyy-MM-dd HH:mm"), days.ToString()));
                        }
                    }
                    return list;
                })
                .OrderByDescending(e => e.When)
                .Select(e => new
                {
                    e.Username,
                    Action = new[] { e.Action },
                    e.When,
                    e.DaysJoined
                })
                .ToList();

            var periodContent = stargazersQuery.Loading
                ? (object)Text.Block("Loading stargazer changes...").Muted()
                : periodEvents.Count == 0
                    ? (object)Text.Block("No joins or leaves in the selected period.").Muted()
                    : periodEvents.AsQueryable()
                        .ToDataTable(idSelector: e => e.Username)
                        .Height(Size.Units(120))
                        .Header(e => e.Username, "User")
                        .Header(e => e.Action, "Action")
                        .Header(e => e.When, "When")
                        .Header(e => e.DaysJoined, "Days")
                        .RowActions(
                            MenuItem.Default(Icons.User, "profile").Tag("profile"),
                            MenuItem.Default(Icons.Eye, "view").Tag("view")
                        )
                        .OnRowAction(e =>
                        {
                            var args = e.Value;
                            var tag = args.Tag?.ToString();
                            var username = args.Id?.ToString();
                            if (string.IsNullOrEmpty(username)) return ValueTask.CompletedTask;
                            if (tag == "view")
                            {
                                var sg = stargazers.FirstOrDefault(s => s.Username == username);
                                if (sg != null) selectedStargazer.Set(sg);
                            }
                            else if (tag == "profile")
                            {
                                navigator.Navigate($"https://github.com/{username}");
                            }
                            return ValueTask.CompletedTask;
                        })
                        .Config(c =>
                        {
                            c.AllowSorting = true;
                            c.AllowFiltering = true;
                            c.ShowSearch = true;
                        });

            var refreshDbButton = new Button(isUpdatingDatabase.Value ? "Updating..." : "Refresh Stargazers")
                .Icon(Icons.RefreshCcw)
                .Variant(ButtonVariant.Outline)
                .Disabled(isUpdatingDatabase.Value)
                .OnClick(async _ =>
                {
                    isUpdatingDatabase.Set(true);
                    try
                    {
                        var result = await updateService.UpdateStargazersAsync();
                        if (result.Success)
                        {
                            // Revalidate related queries
                            starsStatsQuery.Mutator.Revalidate();
                            await Task.Delay(1000);
                            stargazerRefreshVersion.Set(stargazerRefreshVersion.Value + 1);
                        }
                        else
                        {
                            client.Toast($"Update failed: {result.ErrorMessage}");
                        }
                    }
                    finally
                    {
                        isUpdatingDatabase.Set(false);
                    }
                });

            stargazersTodayDialog = new Dialog(
                onClose: (Event<Dialog> _) => showStargazersTodayDialog.Set(false),
                header: new DialogHeader("GitHub Stargazer Activity"),
                body: new DialogBody(Layout.Vertical()
                    .Key($"{fromDate:O}_{toDate:O}_{stargazerRefreshVersion.Value}")
                    | (Layout.Horizontal()
                        | stargazersDateRange.ToDateRangeInput()
                            .Placeholder("Select period")
                            .Format("MMM dd, yyyy")
                        | refreshDbButton)
                    | periodContent),
                footer: new DialogFooter(
                    new Button("Close").OnClick(_ => showStargazersTodayDialog.Set(false)))
            ).Width(Size.Units(220));
        }

        Dialog? stargazerDetailDialog = null;
        if (selectedStargazer.Value is { } sg)
        {
            var daysAsStargazer = sg.IsActive && sg.StarredAt.HasValue
                ? (DateTime.UtcNow.Date - sg.StarredAt.Value.Date).Days
                : sg.StarredAt.HasValue && sg.UnstarredAt.HasValue
                    ? (sg.UnstarredAt.Value.Date - sg.StarredAt.Value.Date).Days
                    : (int?)null;

            var detailModel = new
            {
                Status = sg.IsActive ? "Active" : "Left",
                JoinedAt = sg.StarredAt?.ToString("MMM dd, yyyy HH:mm"),
                LeftAt = sg.UnstarredAt?.ToString("MMM dd, yyyy HH:mm"),
                DaysAsStargazer = daysAsStargazer.HasValue ? $"{daysAsStargazer.Value} days" : null
            };

            stargazerDetailDialog = new Dialog(
                onClose: (Event<Dialog> _) => selectedStargazer.Set((GithubStargazer?)null),
                header: new DialogHeader(sg.Username),
                body: new DialogBody(detailModel.ToDetails().RemoveEmpty()),
                footer: new DialogFooter(
                    new Button("Close").OnClick(_ => selectedStargazer.Set((GithubStargazer?)null)))
            ).Width(Size.Rem(28));
        }

        var openApiButton = new Button("OpenAPI spec")
            .Icon(Icons.FileCode)
            .Variant(ButtonVariant.Outline)
            .Large()
            .Url("/scalar");

        return Layout.Vertical().AlignContent(Align.TopCenter)
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Grid().Columns(3).Width(Size.Fraction(0.9f))
                | adoptionCard
                // | monthlyDownloadsCard
                | dailyActivityCard
                | weeklyGrowthCard)
            | (Layout.Horizontal().Width(Size.Fraction(0.9f)).Height(Size.Units(140))
                | versionChartCard
                | totalDownloadsCard)
            | versionsTableCard
            | new FloatingPanel(openApiButton, Align.BottomRight).Offset(new Thickness(0, 0, 10, 5))
            | (Layout.Horizontal().Width(Size.Fraction(0.9f)).Height(Size.Units(140))
                | githubStarsCard
                | stargazersDailyCard)
            | stargazersTodayDialog
            | stargazerDetailDialog;
    }
}
