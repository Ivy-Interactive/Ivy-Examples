using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class LanguageAnalysisView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public LanguageAnalysisView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var languages = UseState<List<LanguageInfo>?>(() => null);
        var recentCommits = UseState<List<CommitInfo>?>(() => null);
        var isLoading = UseState(true);
        var error = UseState<string?>(() => null);

        UseEffect(async () =>
        {
            try
            {
                isLoading.Set(true);
                error.Set(null);
                
                var languagesTask = _gitHubService.GetLanguagesAsync(_owner, _repo);
                var recentCommitsTask = _gitHubService.GetRecentCommitsAsync(_owner, _repo, 100);
                
                await Task.WhenAll(languagesTask, recentCommitsTask);
                
                var languagesResponse = await languagesTask;
                var recentCommitsResponse = await recentCommitsTask;
                
                if (languagesResponse.Success)
                {
                    languages.Set(languagesResponse.Data);
                }
                
                if (recentCommitsResponse.Success)
                {
                    recentCommits.Set(recentCommitsResponse.Data);
                }
                
                if (!languagesResponse.Success && !recentCommitsResponse.Success)
                {
                    error.Set("Failed to load language data");
                }
            }
            catch (Exception ex)
            {
                error.Set($"Error: {ex.Message}");
            }
            finally
            {
                isLoading.Set(false);
            }
        }, [_refreshTrigger]);

        if (isLoading.Value)
        {
            return LoadingState();
        }

        if (error.Value != null)
        {
            return ErrorState(error.Value);
        }

        return LanguageAnalysisContent(languages.Value, recentCommits.Value);
    }

    private object LoadingState()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | Layout.Grid().Columns(1).Gap(3)
                | SkeletonCard("Language Distribution")
                | SkeletonChart("Language Breakdown")
            | Layout.Grid().Columns(2).Gap(3)
                | SkeletonChart("Top Languages")
                | SkeletonChart("Language Trends");
    }

    private object ErrorState(string errorMessage)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Icon(Icons.AlertTriangle).Color(Colors.Red)
                    | Text.Error("Failed to load language data")
                    | Text.Muted(errorMessage);
    }

    private object LanguageAnalysisContent(List<LanguageInfo>? languages, List<CommitInfo>? recentCommits)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | LanguageSummary(languages)
            | Layout.Grid().Columns(2).Gap(3)
                | LanguageDistributionChart(languages)
                | TopLanguagesChart(languages)
            | LanguageDetailsTable(languages)
            | LanguageTrendsChart(recentCommits);
    }

    private object LanguageSummary(List<LanguageInfo>? languages)
    {
        if (languages == null || !languages.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Summary")
                    | Text.Muted("No language data available");
        }

        var totalBytes = languages.Sum(l => l.Bytes);
        var primaryLanguage = languages.OrderByDescending(l => l.Bytes).First();
        var languageCount = languages.Count;
        var topLanguagePercentage = primaryLanguage.Percentage;

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Language Summary")
                | Layout.Grid().Columns(4).Gap(3)
                    | MetricCard("Total Languages", Icons.Code, languageCount.ToString(), null)
                    | MetricCard("Primary Language", Icons.Star, primaryLanguage.Name, null)
                    | MetricCard("Primary %", Icons.Percent, $"{topLanguagePercentage:F1}%", null)
                    | MetricCard("Total Size", Icons.HardDrive, FormatSize(totalBytes), null);
    }

    private object LanguageDistributionChart(List<LanguageInfo>? languages)
    {
        if (languages == null || !languages.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Distribution")
                    | Text.Muted("No language data available");
        }

        var topLanguages = languages
            .OrderByDescending(l => l.Bytes)
            .Take(8)
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Language Distribution (Top 8)")
                | topLanguages.ToPieChart()
                    .Dimension("Language", e => e.Name)
                    .Measure("Bytes", e => e.Sum(f => f.Bytes))
                    .Height(25)
                    .ColorScheme(ColorScheme.Rainbow)
                    .Tooltip()
                    .Legend();
    }

    private object TopLanguagesChart(List<LanguageInfo>? languages)
    {
        if (languages == null || !languages.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Top Languages")
                    | Text.Muted("No language data available");
        }

        var topLanguages = languages
            .OrderByDescending(l => l.Bytes)
            .Take(10)
            .Select(l => new { Language = l.Name, Percentage = l.Percentage })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Top 10 Languages by Percentage")
                | topLanguages.ToBarChart()
                    .Dimension("Language", e => e.Language)
                    .Measure("Percentage", e => e.Sum(f => f.Percentage))
                    .Height(20)
                    .ColorScheme(ColorScheme.Rainbow)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip();
    }

    private object LanguageDetailsTable(List<LanguageInfo>? languages)
    {
        if (languages == null || !languages.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Details")
                    | Text.Muted("No language data available");
        }

        var tableData = languages
            .OrderByDescending(l => l.Bytes)
            .Select(l => new
            {
                Language = l.Name,
                Bytes = l.Bytes,
                FormattedSize = FormatSize(l.Bytes),
                Percentage = l.Percentage,
                FormattedPercentage = $"{l.Percentage:F2}%"
            })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("All Languages")
                | Table(tableData)
                    .Column("Language", e => e.Language)
                    .Column("Size", e => e.FormattedSize)
                    .Column("Percentage", e => e.FormattedPercentage)
                    .Height(20);
    }

    private object LanguageTrendsChart(List<CommitInfo>? recentCommits)
    {
        if (recentCommits == null || !recentCommits.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Trends")
                    | Text.Muted("No recent commits data available");
        }

        // Simulate language trends based on commit messages and file extensions
        var languageTrends = recentCommits
            .GroupBy(c => c.CommitDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("MMM dd"),
                CSharp = g.Count(c => c.Message.Contains("C#") || c.Message.Contains("csharp") || c.Message.Contains(".cs")),
                TypeScript = g.Count(c => c.Message.Contains("TypeScript") || c.Message.Contains("ts") || c.Message.Contains(".ts")),
                JavaScript = g.Count(c => c.Message.Contains("JavaScript") || c.Message.Contains("js") || c.Message.Contains(".js")),
                PowerShell = g.Count(c => c.Message.Contains("PowerShell") || c.Message.Contains("ps1") || c.Message.Contains(".ps1")),
                CSS = g.Count(c => c.Message.Contains("CSS") || c.Message.Contains("css") || c.Message.Contains(".css"))
            })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Language Activity Trends (Last 30 Days)")
                | languageTrends.ToLineChart(style: LineChartStyles.Dashboard)
                    .Dimension("Date", e => e.Date)
                    .Measure("C#", e => e.Sum(f => f.CSharp))
                    .Measure("TypeScript", e => e.Sum(f => f.TypeScript))
                    .Measure("JavaScript", e => e.Sum(f => f.JavaScript))
                    .Measure("PowerShell", e => e.Sum(f => f.PowerShell))
                    .Measure("CSS", e => e.Sum(f => f.CSS))
                    .Height(20)
                    .ColorScheme(ColorScheme.Rainbow)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip()
                    .Legend();
    }

    private object MetricCard(string title, string icon, string value, double? trend)
    {
        return Card()
            | Layout.Vertical().Gap(2)
                | Layout.Horizontal().Gap(2).AlignCenter()
                    | Icon(icon).Color(Colors.Blue)
                    | Text.Small(title)
                | Text.Large(value)
                | (trend.HasValue ? TrendIndicator(trend.Value) : Spacer());
    }

    private object TrendIndicator(double trend)
    {
        var isPositive = trend > 0;
        var color = isPositive ? Colors.Green : Colors.Red;
        var icon = isPositive ? Icons.TrendingUp : Icons.TrendingDown;
        var text = $"{(trend * 100):+0.0}%";

        return Layout.Horizontal().Gap(1).AlignCenter()
            | Icon(icon).Color(color).Size(Size.Small)
            | Text.Small(text).Color(color);
    }

    private object SkeletonCard(string title)
    {
        return Card()
            | Layout.Vertical().Gap(2)
                | Text.Medium(title)
                | Skeleton().Height(10).Width(100);
    }

    private object SkeletonChart(string title)
    {
        return Card()
            | Layout.Vertical().Gap(2)
                | Text.Medium(title)
                | Skeleton().Height(15).Width(100);
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
