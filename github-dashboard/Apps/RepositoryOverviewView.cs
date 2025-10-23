using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class RepositoryOverviewView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public RepositoryOverviewView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var repositoryData = UseState<RepositoryInfo?>(() => null);
        var isLoading = UseState(true);
        var error = UseState<string?>(() => null);

        UseEffect(async () =>
        {
            try
            {
                isLoading.Set(true);
                error.Set(null);
                
                var response = await _gitHubService.GetRepositoryInfoAsync(_owner, _repo);
                
                if (response.Success)
                {
                    repositoryData.Set(response.Data);
                }
                else
                {
                    error.Set(response.ErrorMessage ?? "Failed to load repository data");
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

        if (repositoryData.Value == null)
        {
            return Text.Error("No repository data available");
        }

        return RepositoryMetrics(repositoryData.Value);
    }

    private object LoadingState()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | Layout.Grid().Columns(2).Gap(3)
                | SkeletonCard("Stars")
                | SkeletonCard("Forks")
                | SkeletonCard("Watchers")
                | SkeletonCard("Issues")
            | Layout.Grid().Columns(1).Gap(3)
                | SkeletonCard("Repository Info");
    }

    private object ErrorState(string errorMessage)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Icon(Icons.AlertTriangle).Color(Colors.Red)
                    | Text.Error("Failed to load repository data")
                    | Text.Muted(errorMessage);
    }

    private object RepositoryMetrics(RepositoryInfo repo)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | RepositoryInfoCard(repo)
            | Layout.Grid().Columns(2).Gap(3)
                | MetricCard("Stars", Icons.Star, repo.Stars.ToString("N0"), CalculateTrend(repo.Stars, 1000))
                | MetricCard("Forks", Icons.GitFork, repo.Forks.ToString("N0"), CalculateTrend(repo.Forks, 50))
                | MetricCard("Watchers", Icons.Eye, repo.Watchers.ToString("N0"), CalculateTrend(repo.Watchers, 100))
                | MetricCard("Open Issues", Icons.AlertCircle, repo.OpenIssues.ToString("N0"), CalculateTrend(repo.OpenIssues, 20))
            | Layout.Grid().Columns(3).Gap(3)
                | MetricCard("Size", Icons.HardDrive, FormatSize(repo.Size), null)
                | MetricCard("Language", Icons.Code, repo.Language ?? "Unknown", null)
                | MetricCard("Last Updated", Icons.Clock, FormatDate(repo.UpdatedAt), null);
    }

    private object RepositoryInfoCard(RepositoryInfo repo)
    {
        return Card()
            | Layout.Vertical().Gap(3)
                | Layout.Horizontal().JustifyBetween().AlignCenter()
                    | Text.H3(repo.Name)
                    | Badge.Success(repo.IsPrivate ? "Private" : "Public")
                | Text.Muted(repo.Description ?? "No description available")
                | Layout.Horizontal().Gap(4)
                    | Layout.Horizontal().Gap(1).AlignCenter()
                        | Icon(Icons.Calendar)
                        | Text.Small($"Created: {FormatDate(repo.CreatedAt)}")
                    | Layout.Horizontal().Gap(1).AlignCenter()
                        | Icon(Icons.GitBranch)
                        | Text.Small($"Default branch: {repo.DefaultBranch}")
                | Button("View on GitHub", () => { })
                    .Variant(ButtonVariants.Outline)
                    .Width(20);
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
                | Text.Small(title)
                | Skeleton().Height(2).Width(15)
                | Skeleton().Height(1).Width(10);
    }

    private double CalculateTrend(int current, int baseline)
    {
        if (baseline == 0) return 0;
        return (double)(current - baseline) / baseline;
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

    private string FormatDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }
}
