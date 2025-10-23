using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class ActivityTrendsView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public ActivityTrendsView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var commitActivity = UseState<Dictionary<string, int>?>(() => null);
        var recentCommits = UseState<List<CommitInfo>?>(() => null);
        var isLoading = UseState(true);
        var error = UseState<string?>(() => null);

        UseEffect(async () =>
        {
            try
            {
                isLoading.Set(true);
                error.Set(null);
                
                var commitActivityTask = _gitHubService.GetCommitActivityAsync(_owner, _repo, 12);
                var recentCommitsTask = _gitHubService.GetRecentCommitsAsync(_owner, _repo, 30);
                
                await Task.WhenAll(commitActivityTask, recentCommitsTask);
                
                var commitActivityResponse = await commitActivityTask;
                var recentCommitsResponse = await recentCommitsTask;
                
                if (commitActivityResponse.Success)
                {
                    commitActivity.Set(commitActivityResponse.Data);
                }
                
                if (recentCommitsResponse.Success)
                {
                    recentCommits.Set(recentCommitsResponse.Data);
                }
                
                if (!commitActivityResponse.Success && !recentCommitsResponse.Success)
                {
                    error.Set("Failed to load activity data");
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

        return ActivityCharts(commitActivity.Value, recentCommits.Value);
    }

    private object LoadingState()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | Layout.Grid().Columns(1).Gap(3)
                | SkeletonChart("Commit Activity")
                | SkeletonChart("Recent Commits")
            | Layout.Grid().Columns(2).Gap(3)
                | SkeletonChart("Commit Distribution")
                | SkeletonChart("Author Activity");
    }

    private object ErrorState(string errorMessage)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Icon(Icons.AlertTriangle).Color(Colors.Red)
                    | Text.Error("Failed to load activity data")
                    | Text.Muted(errorMessage);
    }

    private object ActivityCharts(Dictionary<string, int>? commitActivity, List<CommitInfo>? recentCommits)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | CommitActivityChart(commitActivity)
            | Layout.Grid().Columns(2).Gap(3)
                | RecentCommitsChart(recentCommits)
                | CommitDistributionChart(recentCommits)
            | AuthorActivityChart(recentCommits);
    }

    private object CommitActivityChart(Dictionary<string, int>? commitActivity)
    {
        if (commitActivity == null || !commitActivity.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Commit Activity (Last 12 Weeks)")
                    | Text.Muted("No commit activity data available");
        }

        var chartData = commitActivity
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new { Week = kvp.Key, Commits = kvp.Value })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Commit Activity (Last 12 Weeks)")
                | chartData.ToLineChart(style: LineChartStyles.Dashboard)
                    .Dimension("Week", e => e.Week)
                    .Measure("Commits", e => e.Sum(f => f.Commits))
                    .Height(20)
                    .ColorScheme(ColorScheme.Default)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip()
                    .Legend();
    }

    private object RecentCommitsChart(List<CommitInfo>? recentCommits)
    {
        if (recentCommits == null || !recentCommits.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Recent Commits")
                    | Text.Muted("No recent commits data available");
        }

        var commitsByDate = recentCommits
            .GroupBy(c => c.CommitDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key.ToString("MMM dd"), Count = g.Count() })
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Commits by Date (Last 30 Days)")
                | commitsByDate.ToBarChart()
                    .Dimension("Date", e => e.Date)
                    .Measure("Commits", e => e.Sum(f => f.Count))
                    .Height(15)
                    .ColorScheme(ColorScheme.Default)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip();
    }

    private object CommitDistributionChart(List<CommitInfo>? recentCommits)
    {
        if (recentCommits == null || !recentCommits.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Commit Distribution")
                    | Text.Muted("No commits data available");
        }

        var authorStats = recentCommits
            .GroupBy(c => c.AuthorName)
            .Select(g => new { Author = g.Key, Commits = g.Count() })
            .OrderByDescending(a => a.Commits)
            .Take(10)
            .ToArray();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Top Contributors (Last 30 Days)")
                | authorStats.ToBarChart()
                    .Dimension("Author", e => e.Author)
                    .Measure("Commits", e => e.Sum(f => f.Commits))
                    .Height(15)
                    .ColorScheme(ColorScheme.Rainbow)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip();
    }

    private object AuthorActivityChart(List<CommitInfo>? recentCommits)
    {
        if (recentCommits == null || !recentCommits.Any())
        {
            return Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Author Activity Timeline")
                    | Text.Muted("No commits data available");
        }

        var timelineData = recentCommits
            .GroupBy(c => new { Date = c.CommitDate.Date, Author = c.AuthorName })
            .Select(g => new { 
                Date = g.Key.Date.ToString("MMM dd"), 
                Author = g.Key.Author, 
                Commits = g.Count() 
            })
            .OrderBy(d => d.Date)
            .ToArray();

        var authors = timelineData.Select(d => d.Author).Distinct().Take(5).ToList();

        return Card()
            | Layout.Vertical().Gap(3)
                | Text.Medium("Author Activity Timeline (Top 5 Contributors)")
                | timelineData.ToLineChart(style: LineChartStyles.Dashboard)
                    .Dimension("Date", e => e.Date)
                    .Measure("Commits", e => e.Sum(f => f.Commits))
                    .Height(20)
                    .ColorScheme(ColorScheme.Rainbow)
                    .CartesianGrid(new CartesianGrid().Horizontal())
                    .Tooltip()
                    .Legend();
    }

    private object SkeletonChart(string title)
    {
        return Card()
            | Layout.Vertical().Gap(2)
                | Text.Medium(title)
                | Skeleton().Height(15).Width(100);
    }
}
