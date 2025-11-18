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
        var commits = this.UseState<List<CommitInfo>?>(() => null);
        var isLoading = this.UseState(true);

        this.UseEffect(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    isLoading.Set(true);
                    var response = await _gitHubService.GetRecentCommitsAsync(_owner, _repo, 30);
                    
                    if (response.Success)
                    {
                        commits.Set(response.Data);
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
            });
        });

        if (isLoading.Value)
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Activity Trends")
                | Text.Muted("Loading commit data...");
        }

        if (commits.Value == null || !commits.Value.Any())
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Activity Trends")
                | Text.Muted("No commit data available");
        }

        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | RecentCommitsCard(commits.Value)
            | CommitStatsCard(commits.Value);
    }

    private object RecentCommitsCard(List<CommitInfo> commits)
    {
        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Recent Commits")
                | Layout.Vertical().Gap(2)
                    | commits.Take(10).Select(commit => 
                        Layout.Horizontal().Gap(3)
                            | Text.Small(commit.CommitDate.ToString("MMM dd, yyyy HH:mm"))
                            | Text.P(commit.Message.Split('\n')[0])
                            | Text.Muted($"by {commit.AuthorName}")
                    ).ToArray()
        ).Title("Recent Commits");
    }

    private object CommitStatsCard(List<CommitInfo> commits)
    {
        var commitsByDay = commits
            .GroupBy(c => c.CommitDate.Date)
            .OrderByDescending(g => g.Key)
            .Take(7)
            .ToList();

        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Commits by Day (Last 7 Days)")
                | Layout.Vertical().Gap(2)
                    | commitsByDay.Select(dayGroup => 
                        Layout.Horizontal().Gap(3)
                            | Text.Small(dayGroup.Key.ToString("MMM dd"))
                            | Text.P($"{dayGroup.Count()} commits")
                            | Layout.Horizontal().Gap(1)
                                | dayGroup.Take(5).Select(commit => 
                                    Text.Small($"• {commit.AuthorName}")
                                ).ToArray()
                    ).ToArray()
        ).Title("Daily Commit Activity");
    }
}