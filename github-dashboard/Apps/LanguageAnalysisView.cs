using GitHubDashboard.Services;

namespace GitHubDashboard.Apps;

public class LanguageAnalysisView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;

    public LanguageAnalysisView(IGitHubApiService gitHubService, string owner, string repo)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
    }

    public override object? Build()
    {
        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | Text.Muted("Language analysis will be implemented here")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Distribution")
                    | Text.Small("This will show language distribution")
            | Card()
                | Layout.Vertical().Gap(2)
                    | Text.Medium("Language Trends")
                    | Text.Small("This will show language trends over time");
    }
}