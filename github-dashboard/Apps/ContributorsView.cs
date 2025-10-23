using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class ContributorsView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public ContributorsView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var contributors = UseState<List<ContributorInfo>?>(() => null);
        var recentCommits = UseState<List<CommitInfo>?>(() => null);
        var isLoading = UseState(true);
        var error = UseState<string?>(() => null);

        UseEffect(async () =>
        {
            try
            {
                isLoading.Set(true);
                error.Set(null);
                
                var contributorsTask = _gitHubService.GetContributorsAsync(_owner, _repo);
                var recentCommitsTask = _gitHubService.GetRecentCommitsAsync(_owner, _repo, 100);
                
                await Task.WhenAll(contributorsTask, recentCommitsTask);
                
                var contributorsResponse = await contributorsTask;
                var recentCommitsResponse = await recentCommitsTask;
                
                if (contributorsResponse.Success)
                {
                    contributors.Set(contributorsResponse.Data);
                }
                
                if (recentCommitsResponse.Success)
                {
                    recentCommits.Set(recentCommitsResponse.Data);
                }
                
                if (!contributorsResponse.Success && !recentCommitsResponse.Success)
                {
                    error.Set("Failed to load contributors data");
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

        return ContributorsContent(contributors.Value, recentCommits.Value);
    }

    private object LoadingState()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | Layout.Grid().Columns(1).Gap(3)
                | SkeletonCard("Contributors List")
                | SkeletonChart("Contribution Distribution")
            | Layout.Grid().Columns(2).Gap(3)
                | SkeletonChart("Top Contributors")
                | SkeletonChart("Contribution Timeline");
    }

    private object ErrorState(string errorMessage)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Icon(Icons.AlertTriangle).Color(Colors.Red)
                    | Text.Error("Failed to load contributors data")
                    | Text.Muted(errorMessage);
    }

    private object ContributorsContent(List<ContributorInfo>? contributors, List<CommitInfo>? recentCommits)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | ContributorsSummary(contributors)
            | Layout.Grid().Columns(2).Gap(3)
                | TopContributorsChart(contributors)
                | ContributionDistributionChart(contributors)
            | ContributorsTable(contributors)
            | RecentActivityChart(recentCommits);
    }

    private object ContributorsSummary(List<ContributorInfo>? contributors)
    {
        if (contributors == null || !contributors.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Contributors Summary")
                    | Text.Muted("No contributors data available");
        }

        var totalContributors = contributors.Count;
        var totalContributions = contributors.Sum(c => c.Contributions);
        var avgContributions = totalContributions / (double)totalContributors;
        var topContributor = contributors.OrderByDescending(c => c.Contributions).First();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Contributors Summary")
                | Layout.Grid().Columns(4).Gap(3)
                    | MetricCard("Total Contributors", Icons.Users, totalContributors.ToString("N0"), null)
                    | MetricCard("Total Contributions", Icons.GitCommit, totalContributions.ToString("N0"), null)
                    | MetricCard("Avg per Contributor", Icons.BarChart3, avgContributions.ToString("F1"), null)
                    | MetricCard("Top Contributor", Icons.Crown, topContributor.Login, null);
    }

    private object TopContributorsChart(List<ContributorInfo>? contributors)
    {
        if (contributors == null || !contributors.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Top Contributors")
                    | Text.Muted("No contributors data available");
        }

        var topContributors = contributors
            .OrderByDescending(c => c.Contributions)
            .Take(10)
            .Select(c => new { Contributor = c.Login, Contributions = c.Contributions })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Top 10 Contributors")
                | topContributors.ToBarChart()
                    .Dimension("Contributor", e => e.Contributor)
                    .Measure("Contributions", e => e.Sum(f => f.Contributions))
                    .Height(20)
                    .ColorScheme(ColorScheme.Rainbow)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip();
    }

    private object ContributionDistributionChart(List<ContributorInfo>? contributors)
    {
        if (contributors == null || !contributors.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Contribution Distribution")
                    | Text.Muted("No contributors data available");
        }

        var distribution = contributors
            .GroupBy(c => GetContributionTier(c.Contributions))
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .OrderBy(d => d.Tier)
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Contribution Distribution")
                | distribution.ToPieChart()
                    .Dimension("Tier", e => e.Tier)
                    .Measure("Count", e => e.Sum(f => f.Count))
                    .Height(20)
                    .ColorScheme(ColorScheme.Rainbow)
                    .Tooltip()
                    .Legend();
    }

    private object ContributorsTable(List<ContributorInfo>? contributors)
    {
        if (contributors == null || !contributors.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Contributors List")
                    | Text.Muted("No contributors data available");
        }

        var tableData = contributors
            .OrderByDescending(c => c.Contributions)
            .Take(20)
            .Select(c => new
            {
                Rank = contributors.OrderByDescending(cc => cc.Contributions).ToList().IndexOf(c) + 1,
                Username = c.Login,
                Contributions = c.Contributions,
                Type = c.Type,
                Avatar = c.AvatarUrl
            })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Top 20 Contributors")
                | Table(tableData)
                    .Column("Rank", e => e.Rank.ToString())
                    .Column("Username", e => e.Username)
                    .Column("Contributions", e => e.Contributions.ToString("N0"))
                    .Column("Type", e => e.Type)
                    .Height(25);
    }

    private object RecentActivityChart(List<CommitInfo>? recentCommits)
    {
        if (recentCommits == null || !recentCommits.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Recent Activity")
                    | Text.Muted("No recent commits data available");
        }

        var activityData = recentCommits
            .GroupBy(c => new { Date = c.CommitDate.Date, Author = c.AuthorName })
            .Select(g => new { 
                Date = g.Key.Date.ToString("MMM dd"), 
                Author = g.Key.Author, 
                Commits = g.Count() 
            })
            .OrderBy(d => d.Date)
            .ToArray();

        var topAuthors = activityData
            .GroupBy(a => a.Author)
            .OrderByDescending(g => g.Sum(a => a.Commits))
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var filteredData = activityData
            .Where(a => topAuthors.Contains(a.Author))
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Recent Activity (Top 5 Contributors)")
                | filteredData.ToLineChart(style: LineChartStyles.Dashboard)
                    .Dimension("Date", e => e.Date)
                    .Measure("Commits", e => e.Sum(f => f.Commits))
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

    private string GetContributionTier(int contributions)
    {
        return contributions switch
        {
            >= 1000 => "1000+",
            >= 500 => "500-999",
            >= 100 => "100-499",
            >= 50 => "50-99",
            >= 10 => "10-49",
            _ => "1-9"
        };
    }
}
