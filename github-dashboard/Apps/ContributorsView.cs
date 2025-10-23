using GitHubDashboard.Services;

namespace GitHubDashboard.Apps;

public class ContributorsView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;

    public ContributorsView(IGitHubApiService gitHubService, string owner, string repo)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
    }

    public override object? Build()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | Text.Muted("Contributor statistics will be implemented here")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Top Contributors")
                    | Text.Small("This will show top contributors")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Contribution Distribution")
                    | Text.Small("This will show contribution distribution");
    }
}