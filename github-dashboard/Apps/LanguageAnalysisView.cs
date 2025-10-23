using GitHubDashboard.Services;

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
        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | Text.Muted("Language analysis will be implemented here")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Language Distribution")
                    | Text.Small("This will show language distribution")
            ).Title("Language Distribution")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P("Language Trends")
                    | Text.Small("This will show language trends over time")
            ).Title("Language Trends");
    }
}