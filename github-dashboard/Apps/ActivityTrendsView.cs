using GitHubDashboard.Services;

namespace GitHubDashboard.Apps;

public class ActivityTrendsView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;

    public ActivityTrendsView(IGitHubApiService gitHubService, string owner, string repo)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
    }

    public override object? Build()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Activity Trends")
            | Text.Muted("Activity trends will be implemented here")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Commit Activity")
                    | Text.Small("This will show commit activity over time")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Recent Commits")
                    | Text.Small("This will show recent commits");
    }
}