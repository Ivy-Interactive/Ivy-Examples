using GitHubDashboard.Services;

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
        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | Text.Muted("Activity trends will be implemented here")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Commit Activity")
                    | Text.Small("This will show commit activity over time")
            ).Title("Commit Activity")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Recent Commits")
                    | Text.Small("This will show recent commits")
            ).Title("Recent Commits");
    }
}