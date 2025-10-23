using GitHubDashboard.Services;

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
        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | Text.Muted("Contributor statistics will be implemented here")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Top Contributors")
                    | Text.Small("This will show top contributors")
            ).Title("Top Contributors")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Contribution Distribution")
                    | Text.Small("This will show contribution distribution")
            ).Title("Contribution Distribution");
    }
}