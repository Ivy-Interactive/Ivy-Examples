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
        var repositoryData = this.UseState<RepositoryInfo?>(() => null);
        var isLoading = this.UseState(true);

        this.UseEffect(async () =>
        {
            try
            {
                isLoading.Set(true);
                var response = await _gitHubService.GetRepositoryInfoAsync(_owner, _repo);
                
                if (response.Success)
                {
                    repositoryData.Set(response.Data);
                }
            }
            catch (Exception)
            {
                // Handle error silently for now
            }
            finally
            {
                isLoading.Set(false);
            }
        }, _refreshTrigger);

        if (isLoading.Value)
        {
            return LoadingState();
        }

        if (repositoryData.Value == null)
        {
            return ErrorState("Failed to load repository data");
        }

        return RepositoryMetrics(repositoryData.Value);
    }

    private object LoadingState()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | Text.Muted("Loading repository data...");
    }

    private object ErrorState(string errorMessage)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | Text.Danger(errorMessage);
    }

    private object RepositoryMetrics(RepositoryInfo repo)
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Repository Overview")
            | RepositoryInfoCard(repo)
            | Layout.Grid().Columns(2).Gap(3)
                | MetricCard("Stars", repo.Stars.ToString("N0"))
                | MetricCard("Forks", repo.Forks.ToString("N0"))
                | MetricCard("Watchers", repo.Watchers.ToString("N0"))
                | MetricCard("Open Issues", repo.OpenIssues.ToString("N0"))
            | Layout.Grid().Columns(3).Gap(3)
                | MetricCard("Size", FormatSize(repo.Size))
                | MetricCard("Language", repo.Language ?? "Unknown")
                | MetricCard("Last Updated", FormatDate(repo.UpdatedAt));
    }

    private object RepositoryInfoCard(RepositoryInfo repo)
    {
        return new Card(
            Layout.Vertical().Gap(3)
                | Layout.Horizontal().Gap(4)
                    | Text.H3(repo.Name)
                    | new Badge(repo.IsPrivate ? "Private" : "Public", variant: BadgeVariant.Success)
                | Text.Muted(repo.Description ?? "No description available")
                | Layout.Horizontal().Gap(4)
                    | Text.Small($"Created: {FormatDate(repo.CreatedAt)}")
                    | Text.Small($"Default branch: {repo.DefaultBranch}")
        ).Title("Repository Information");
    }

    private object MetricCard(string title, string value)
    {
        return new Card(
            Layout.Vertical().Gap(2)
                | Text.Small(title)
                | Text.Large(value)
        ).Title(title);
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