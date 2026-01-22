using IvyInsights.Models;
using IvyInsights.Services;

namespace IvyInsights.Apps;

[App(icon: Icons.ChartBar, title: "NuGet Statistics")]
public class NuGetStatsApp : ViewBase
{
    private const string PackageId = "Ivy";

    public override object? Build()
    {
        var loading = this.UseState(false);
        var stats = this.UseState<PackageStatistics?>();
        var error = this.UseState<string?>();
        var client = UseService<IClientProvider>();
        var nugetProvider = UseService<INuGetStatisticsProvider>();

        async Task LoadStatisticsAsync()
        {
            loading.Set(true);
            error.Set((string?)null);
            try
            {
                var statistics = await nugetProvider.GetPackageStatisticsAsync(PackageId);
                stats.Set(statistics);
                client.Toast($"Successfully loaded statistics for {PackageId}!");
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
                client.Toast(ex);
            }
            finally
            {
                loading.Set(false);
            }
        }

        // Auto-load on startup
        this.UseEffect(async () =>
        {
            if (stats.Value == null && !loading.Value)
            {
                await LoadStatisticsAsync();
            }
        }, [EffectTrigger.OnMount()]);

        if (error.Value != null)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Error")
                        | Text.Block(error.Value)
                        | new Button("Retry", onClick: () => { _ = LoadStatisticsAsync(); })
                            .Icon(Icons.RefreshCcw)
                ).Width(Size.Fraction(0.5f));
        }

        if (loading.Value || stats.Value == null)
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

        var s = stats.Value!;

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

        // Enhanced KPI cards
        var metrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(totalDownloads.ToString("N0"))
                    | Text.Muted("Total Downloads")
            ).Title("Total Downloads").Icon(Icons.Download)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(s.TotalVersions.ToString("N0"))
                    | Text.Muted("Versions")
            ).Title("Total Versions").Icon(Icons.Tag)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(s.LatestVersion)
                    | Text.Muted(s.LatestVersionPublished.HasValue 
                        ? s.LatestVersionPublished.Value.ToString("MMM dd, yyyy")
                        : "N/A")
            ).Title("Latest Version").Icon(Icons.ArrowUp)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(mostDownloadedVersion != null 
                        ? mostDownloadedVersion.Version 
                        : "N/A")
                    | Text.Muted(mostDownloadedVersion != null
                        ? (mostDownloadedVersion.Downloads.HasValue && mostDownloadedVersion.Downloads.Value > 0
                            ? $"{mostDownloadedVersion.Downloads.Value:N0} downloads"
                            : mostDownloadedVersion.Published.HasValue
                                ? $"Published {mostDownloadedVersion.Published.Value:MMM dd, yyyy}"
                                : "Latest version")
                        : "")
            ).Title("Most Popular").Icon(Icons.Star);

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

        // Version distribution chart
        var versionChartData = s.Versions
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .Take(20)
            .Select(v => new 
            { 
                Version = v.Version, 
                Downloads = (double)(v.Downloads ?? 0),
                HasDownloads = v.Downloads.HasValue && v.Downloads.Value > 0
            })
            .ToList();

        var versionChart = versionChartData.ToBarChart()
            .Dimension("Version", e => e.Version)
            .Measure("Downloads", e => e.Sum(f => f.HasDownloads ? f.Downloads : 1.0));

        var versionChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Recent Versions Distribution")
                | Text.Muted(versionsWithDownloads > 0 
                    ? "Showing downloads per version" 
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

        // Header
        var header = Layout.Horizontal().Gap(2).Align(Align.Right)
            | Text.H1($"NuGet Statistics: {PackageId}")
            | new Spacer()
            | new Button("Refresh", onClick: () => { _ = LoadStatisticsAsync(); })
                .Icon(Icons.RefreshCcw)
                .Loading(loading.Value)
                .Disabled(loading.Value);

        return new HeaderLayout(
            header: header,
            content: Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | metrics.Width(Size.Fraction(0.9f))
                | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                    | packageInfo
                    | versionChartCard)
                | (Layout.Horizontal().Gap(3).Width(Size.Fraction(0.9f))
                    | versionsTableCard
                    | timelineChartCard)
        );
    }
}
